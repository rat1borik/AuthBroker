using AuthBroker.Data;
using AuthBroker.Model;
using AuthBroker.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Diagnostics.Eventing.Reader;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Authorization;
using BlazorBootstrap;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddBlazoredLocalStorage();
         
builder.Services.AddBlazorBootstrap();

builder.Services.AddHttpContextAccessor();
builder.Services.AddOptions();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddScoped<AuthenticationStateProvider,CustomAuthenticationStateProvider > ();
builder.Services.AddAntiforgery();
builder.Services.AddTransient<GrantStore>();
builder.Services.AddTransient<UserAccStore>();
builder.Services.AddTransient<AppClientStore>();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "AuthBroker API", Version = "v1" });
});

builder.Services.AddRazorPages();
var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
	var dbCtx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	dbCtx.Database.EnsureDeleted();
	dbCtx.Database.EnsureCreated();

	dbCtx.Users.Add(new User() { Login = "228775", Password = "123456" });
	dbCtx.SaveChanges();
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();


var prefix = "/api/v1";
app.MapGet(prefix+"/grants", ([FromServices] GrantStore gs) =>
		gs.GetListAsync());



app.MapGet(prefix + "/authorize", [ValidateAntiForgeryToken]  async (HttpContext ctx, [FromServices] AuthenticationStateProvider asp) => {
	var authState = await ((CustomAuthenticationStateProvider)asp).GetAuthenticationStateAsync();
	if (authState.User.Claims != null) {
		return Results.Ok("Пользователь аутентифицирован.");
	} else {
		;
		return Results.Redirect("/login");
	}

});


app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
	c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
});


app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
