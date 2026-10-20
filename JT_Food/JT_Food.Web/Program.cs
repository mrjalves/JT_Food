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
builder.Services.AddScoped<ISecureStorageService, JT_Food.Shared.Services.WebSecureStorageService>();
builder.Services.AddJTFoodServices();

// Acionar o migrador de banco de dados para os módulos que o JT_Food utiliza
var stringConexao = builder.Configuration["Config:Conexao:StringConexao"];
var tipoBanco = builder.Configuration["Config:Conexao:TipoBanco"] ?? "SqlServer";

if (!string.IsNullOrWhiteSpace(stringConexao))
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
    try
    {
        orquestrador.Executar("JT_Food_Auto", configCliente);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[MIGRACAO][ERRO] Falha na migração automática: {ex.Message}");
        Console.WriteLine("[MIGRACAO][AVISO] A aplicação continuará normalmente. Verifique o schema manualmente se necessário.");
    }

    try
    {
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
    }
    catch (Exception exMig)
    {
        Console.WriteLine($"[MIGRACAO][FOOD] Aviso ao migrar tabelas do Food: {exMig.Message}");
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

app.MapGet("/api/gemini/models", async (string? key, IHttpClientFactory factory) =>
{
    if (string.IsNullOrWhiteSpace(key))
    {
        return Results.BadRequest(new { error = new { message = "Chave de API não informada.", reason = "API_KEY_INVALID" } });
    }

    try
    {
        var client = factory.CreateClient("GeminiCleanClient");
        var response = await client.GetAsync($"https://generativelanguage.googleapis.com/v1beta/models?key={Uri.EscapeDataString(key.Trim())}");
        var content = await response.Content.ReadAsStringAsync();
        return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.MapPost("/api/gemini/generate", async (string? key, string? model, HttpContext context, IHttpClientFactory factory) =>
{
    if (string.IsNullOrWhiteSpace(key))
    {
        return Results.BadRequest(new { error = new { message = "Chave de API não informada.", reason = "API_KEY_INVALID" } });
    }

    try
    {
        string modelo = !string.IsNullOrWhiteSpace(model) ? model : "gemini-1.5-flash";
        var client = factory.CreateClient("GeminiCleanClient");
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/{modelo}:generateContent?key={Uri.EscapeDataString(key.Trim())}", content);
        var resContent = await response.Content.ReadAsStringAsync();
        return Results.Content(resContent, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
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

