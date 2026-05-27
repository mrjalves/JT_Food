using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped<JT_UserInterface.Shared.Services.Interfaces.IMenuProvider, JT_Food.Shared.Layout.MenuLateral>();
builder.Services.AddScoped<JT_UserInterface.Shared.Services.Interfaces.IConfiguracaoTabProvider, JT_Food.Shared.Services.RestauranteConfigTabProvider>();

await builder.Build().RunAsync();
