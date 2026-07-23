using JT_Aplicacao.Config;
using JT_Classes.Acessos;
using JT_Food.Shared;
using JT_Food.Web.Components;
using JT_UserInterface.Shared;

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

builder.Services.AddHttpClient();
builder.Services.AddScoped(sp =>
{
    var baseUrl = builder.Configuration["Config:FirebaseConfig:BaseApiUrl"] ?? "https://localhost:7166";
    return new HttpClient { BaseAddress = new Uri(baseUrl) };
});

builder.Services.AddJTUserInterface();
builder.Services.AddScoped<ISecureStorageService, JT_Food.Shared.Services.WebSecureStorageService>();
builder.Services.AddJTFoodServices();

var app = builder.Build();

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
app.UseHttpsRedirection();

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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(JT_Food.Shared._Imports).Assembly,
        typeof(JT_Food.Web.Client._Imports).Assembly,
        typeof(JT_UserInterface.Shared._Imports).Assembly
    );

app.Run();
