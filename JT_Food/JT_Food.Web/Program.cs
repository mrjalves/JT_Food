using JT_Aplicacao.Config;
using JT_Classes.Acessos;
using JT_Food.Shared;
using JT_Food.Web.Components;
using JT_UserInterface.Shared;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.Configure<FirebaseConfig>(builder.Configuration.GetSection("Config:FirebaseConfig"));

var secaoConfig = builder.Configuration.GetSection("Config");
var dadosConfig = secaoConfig.Get<Config>();

if (dadosConfig != null)
{
    Config.SetInstancia(dadosConfig);
}

secaoConfig = builder.Configuration.GetSection("Config:FirebaseConfig");
var dadosFConfig = secaoConfig.Get<FirebaseConfig>();

if (dadosFConfig != null)
{
    FirebaseConfig.SetInstancia(dadosFConfig);
    builder.Services.AddSingleton(FirebaseConfig.Instancia);
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHttpClient();
builder.Services.AddHttpClient("GeminiCleanClient");
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient(string.Empty);

    var baseUrl = builder.Configuration["Config:Conexao:BaseApiUrl"]
               ?? builder.Configuration["Config:FirebaseConfig:BaseApiUrl"]
               ?? "https://api.jtecsistemas.com.br";
    httpClient.BaseAddress = new Uri(baseUrl);

    var configPayload = new JT_Aplicacao.Config.ClientConfigPayload { ClientId = "JT_Food" };
    if (JT_Aplicacao.Config.Config.Instancia != null)
    {
        JT_Aplicacao.Config.Config.Instancia.ClientId = configPayload.ClientId;
    }

    var json = System.Text.Json.JsonSerializer.Serialize(configPayload);
    var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
    if (!httpClient.DefaultRequestHeaders.Contains("X-Client-Config"))
    {
        httpClient.DefaultRequestHeaders.Add("X-Client-Config", encoded);
    }

    return httpClient;
});

builder.Services.AddJTUserInterface();
if (Config.Instancia != null)
{
    Config.Instancia.ClientId = "JT_Food";
}
builder.Services.AddScoped<ISecureStorageService, JT_Food.Shared.Services.WebSecureStorageService>();
builder.Services.AddJTFoodServices();

// Migração de schema sob controle explícito (--migrate ou Config:AutoMigrateOnStartup=true)
bool runMigrations = args.Contains("--migrate", StringComparer.OrdinalIgnoreCase)
                  || builder.Configuration.GetValue<bool>("Config:AutoMigrateOnStartup");

var stringConexao = builder.Configuration["Config:Conexao:StringConexao"]
                 ?? Environment.GetEnvironmentVariable("JT_FOOD_CONNECTION_STRING");
var tipoBanco = builder.Configuration["Config:Conexao:TipoBanco"] ?? "SqlServer";

if (runMigrations && !string.IsNullOrWhiteSpace(stringConexao))
{
    var configCliente = new JT_Aplicacao.Config.ConfigCliente
    {
        StringConexao = stringConexao,
        TipoBanco = tipoBanco,
        ModulosAtivos = new List<JT_Aplicacao.Classes.eModuloSistema>
        {
            JT_Aplicacao.Classes.eModuloSistema.Gestao,
            JT_Aplicacao.Classes.eModuloSistema.Restaurante,
            JT_Aplicacao.Classes.eModuloSistema.Food
        }
    };

    var orquestrador = new JT_Infra_Data.Schemas.ModuloMigrationOrchestrator();
    orquestrador.Executar("JT_Food_Auto", configCliente);

    var migrador = new JT_Infra_Data.Schemas.DbSchemaMigrator(stringConexao);
    migrador.AplicarMigracoes(new List<Type>
    {
        typeof(JT_Classes.Restaurante.MItemComanda),
        typeof(JT_Classes.Restaurante.MAdicionaisItemComanda),
        typeof(JT_Classes.Restaurante.MCaixa),
        typeof(JT_Classes.Restaurante.MMovimentacaoCaixa),
        typeof(JT_Classes.Restaurante.MInsumo),
        typeof(JT_Classes.Restaurante.MLoteInsumo),
        typeof(JT_Classes.Restaurante.MReceita),
        typeof(JT_Classes.Restaurante.MProducao),
        typeof(JT_Classes.Restaurante.MPagamentoComanda),
        typeof(JT_Classes.Restaurante.MMemoriaCalculo),
        typeof(JT_Classes.Restaurante.MItemCompra),
        typeof(JT_Classes.Restaurante.MEstoque),
        typeof(JT_Classes.Restaurante.MFoodConfiguracao)
    });
    Console.WriteLine("[MIGRACAO][FOOD] Tabelas do ecossistema Food migradas/verificadas com sucesso.");

    if (args.Contains("--migrate-only", StringComparer.OrdinalIgnoreCase))
    {
        return;
    }
}

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin-allow-popups";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseRouting();
app.UseAntiforgery();
app.MapStaticAssets();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "JT_Food.Web",
    timestamp = DateTime.UtcNow
}));

static string? ResolverGeminiKey(HttpContext ctx, IConfiguration cfg)
{
    if (ctx.Request.Headers.TryGetValue("X-Gemini-Api-Key", out var headerKey) && !string.IsNullOrWhiteSpace(headerKey))
    {
        return headerKey.ToString().Trim();
    }

    return cfg["Config:Gemini:ApiKey"]
        ?? cfg["Gemini:ApiKey"]
        ?? cfg["GeminiApiKey"]
        ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
}

app.MapGet("/api/gemini/models", async (HttpContext context, IConfiguration config, IHttpClientFactory factory) =>
{
    var apiKey = ResolverGeminiKey(context, config);
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        return Results.BadRequest(new { error = new { message = "Chave de API não informada.", reason = "API_KEY_INVALID" } });
    }

    try
    {
        var client = factory.CreateClient("GeminiCleanClient");
        using var req = new HttpRequestMessage(HttpMethod.Get, "https://generativelanguage.googleapis.com/v1beta/models");
        req.Headers.TryAddWithoutValidation("x-goog-api-key", apiKey);
        var response = await client.SendAsync(req, context.RequestAborted);
        var content = await response.Content.ReadAsStringAsync(context.RequestAborted);
        return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.MapPost("/api/gemini/generate", async (string? model, HttpContext context, IConfiguration config, IHttpClientFactory factory) =>
{
    var apiKey = ResolverGeminiKey(context, config);
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        return Results.BadRequest(new { error = new { message = "Chave de API não informada.", reason = "API_KEY_INVALID" } });
    }

    try
    {
        string modelo = !string.IsNullOrWhiteSpace(model) ? model.Trim() : "gemini-1.5-flash";
        var client = factory.CreateClient("GeminiCleanClient");
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync(context.RequestAborted);

        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(modelo)}:generateContent")
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        req.Headers.TryAddWithoutValidation("x-goog-api-key", apiKey);

        var response = await client.SendAsync(req, context.RequestAborted);
        var resContent = await response.Content.ReadAsStringAsync(context.RequestAborted);
        return Results.Content(resContent, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// Endpoints transacionais de retaguarda para execução segura no servidor
var apiOperacoes = app.MapGroup("/api/operacoes");

apiOperacoes.MapPost("/producao/processar", async (
    ProducaoRequest req,
    JT_Food.Shared.BusinessLogic.ProducaoLogic producaoLogic) =>
{
    var (sucesso, mensagem) = await producaoLogic.ProcessarProducaoAsync(req.ProdutoId, req.QuantidadeProduzida, req.ResponsavelId);
    return sucesso ? Results.Ok(new { sucesso, mensagem }) : Results.BadRequest(new { sucesso, mensagem });
});

apiOperacoes.MapPost("/caixa/abrir", async (
    AbrirCaixaRequest req,
    JT_Food.Shared.BusinessLogic.CaixaLogic caixaLogic) =>
{
    var (sucesso, mensagem) = await caixaLogic.AbrirCaixaAsync(req.OperadorId, req.SaldoInicial, req.Observacao);
    return sucesso ? Results.Ok(new { sucesso, mensagem }) : Results.BadRequest(new { sucesso, mensagem });
});

apiOperacoes.MapPost("/caixa/fechar", async (
    FecharCaixaRequest req,
    JT_Food.Shared.BusinessLogic.CaixaLogic caixaLogic) =>
{
    var (sucesso, mensagem) = await caixaLogic.FecharCaixaAsync(req.CaixaId, req.OperadorId, req.SaldoInformado, req.Observacao);
    return sucesso ? Results.Ok(new { sucesso, mensagem }) : Results.BadRequest(new { sucesso, mensagem });
});

apiOperacoes.MapPost("/kds/avancar/{itemId:int}", async (
    int itemId,
    JT_Food.Shared.BusinessLogic.KDSLogic kdsLogic) =>
{
    var (sucesso, mensagem, novoStatus) = await kdsLogic.AvancarStatusItemAsync(itemId);
    return sucesso ? Results.Ok(new { sucesso, mensagem, novoStatus }) : Results.BadRequest(new { sucesso, mensagem, novoStatus });
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(JT_Food.Shared._Imports).Assembly,
        typeof(JT_Food.Web.Client._Imports).Assembly,
        typeof(JT_UserInterface.Shared._Imports).Assembly
    );

app.Run();

public record ProducaoRequest(string ProdutoId, double QuantidadeProduzida, string ResponsavelId);
public record AbrirCaixaRequest(string OperadorId, double SaldoInicial, string? Observacao);
public record FecharCaixaRequest(string CaixaId, string OperadorId, double SaldoInformado, string? Observacao);
