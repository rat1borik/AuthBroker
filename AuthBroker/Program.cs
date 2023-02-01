using AuthBroker.Data;
using AuthBroker.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddDbContext<TestContext>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddTransient<VehicleStore>();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AuthBroker API", Version = "v1" });
});

builder.Services.AddRazorPages();
var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var dbCtx = scope.ServiceProvider.GetRequiredService<TestContext>();
    //dbCtx.Database.EnsureDeleted();
    dbCtx.Database.EnsureCreated();
}

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.MapPost("/v1/init", ([FromServices] VehicleStore vh) =>
        vh.InitAsync());
app.MapGet("/v1/vehicles", ([FromServices] VehicleStore vh) => 
        vh.GetListAsync());


app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
});


app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
