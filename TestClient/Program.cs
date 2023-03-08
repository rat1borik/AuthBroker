using BlazorBootstrap;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Web;
using TestClient.Data;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazorBootstrap();
builder.Services.AddLocalization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddOptions();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddServerSideBlazor();
builder.Services.AddAntiforgery();
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<StateProvider>();

if (builder.Configuration.GetSection("SSO")["ClientId"] == null || builder.Configuration.GetSection("SSO")["ClientSecret"] == null) {
	throw new Exception("No ClientId or ClientSecret in configuration file");
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseRequestLocalization(new RequestLocalizationOptions()
	.AddSupportedCultures(new[] { "en-US", "ru-RU" })
	.AddSupportedUICultures(new[] { "en-US", "ru-RU" }));

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
