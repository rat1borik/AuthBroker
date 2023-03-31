using AuthBroker.Models;
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
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Principal;
using System.Security.Cryptography;
using Bogus.DataSets;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddBlazoredLocalStorage();
		 
builder.Services.AddBlazorBootstrap();
builder.Services.AddLocalization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddOptions();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddServerSideBlazor().AddHubOptions(options =>
{
	options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
	options.HandshakeTimeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddScoped<AuthenticationStateProvider,CustomAuthenticationStateProvider > ();
builder.Services.AddAntiforgery();
builder.Services.AddTransient<GrantStore>();
builder.Services.AddTransient<UserAccStore>();
builder.Services.AddTransient<AppClientStore>();
builder.Services.AddTransient<SessionStore>();
builder.Services.AddTransient<AccessTokenStore>();
builder.Services.AddSingleton<CryptoProvider>();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "AuthBroker API", Version = "v1" });
});

builder.Services.AddRazorPages();

var app = builder.Build();

app.UseRequestLocalization(new RequestLocalizationOptions()
	.AddSupportedCultures(new[] { "en-US", "ru-RU" })
	.AddSupportedUICultures(new[] { "en-US", "ru-RU" }));


RSA signKey = RSA.Create();

using (var scope = app.Services.CreateScope()) {
	var dbCtx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	dbCtx.Database.EnsureDeleted();
	var scp = new Grant() { Id = "e-mail", Name = "E-mail", Action = "read", ValueType = "string" };

	dbCtx.Database.EnsureCreated();
	dbCtx.Grants.Add(scp);
	dbCtx.SaveChanges();

	//var usr = new User() { Login = "228775", Password = "123456" };
	//dbCtx.Users.Add(usr);
	//var appc = new AppClient() { Name = "341342143", AllowedRedirectUris = new Uri[] { new Uri("https://localhost:7156/") } };
	//var scp = new Scope() { Name = "sdfasgdsfa" };
	//var sess = new Session() { User = usr, App = appc, Scopes = new List<Scope>() { scp } };
	//dbCtx.Scopes.Add(scp);
	//dbCtx.AppClients.Add(appc);
	//dbCtx.Sessions.Add(sess);
	//dbCtx.SaveChanges();
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
app.MapGet(prefix+"/grants", async ([FromServices] GrantStore gs) =>
		(await gs.GetListAsync()).Select(gr=>new GrantView() { Id = gr.Id, Name = gr.Name, Action = gr.Action, ValueType = gr.ValueType}));



app.MapPost(prefix + "/token", async (AuthTokenRequest tReq, [FromServices] SessionStore ss, [FromServices] AccessTokenStore ats, [FromServices] AuthenticationStateProvider st, HttpContext ctx) => {

	switch (tReq.GrantType) {
		case "authorization_code":
			var sess = await ss.GetSessionByCode(tReq.Code.Base64UrlDecode());

			if (sess != null && Convert.ToBase64String(sess.App.SecretKey) == tReq.Secret) {

				var at = new AccessToken() {Session = sess, Ip = tReq.RemoteIp, UserAgent = tReq.UserAgent };
				await ats.AddAsync(at);

				var grants  =  sess.Grants.ToList().ConvertAll<Claim>(gr => {
					var cred = sess.User.Credentials[gr.Id];
					if (cred != null) {
						return new Claim(gr.Id, cred, gr.ValueType);
					}
					return null;
				}).Where(cl=>cl!=null);

				var jwt = new JwtSecurityToken(
					issuer: $"{ctx.Request.Scheme}://{ctx.Request.Host}{ctx.Request.PathBase}",
					audience: sess.App.Id.ToString(),
					notBefore: DateTime.Now,
					claims: new List<Claim>(grants) {   new Claim(ClaimsIdentity.DefaultNameClaimType, sess.User.Login)
											, new Claim(ClaimsIdentity.DefaultRoleClaimType, sess.User.IsAdmin?"Admin":"User")
											, new Claim("access_token", at.Token.Key)
											, new Claim("refresh_token", at.RefreshToken.Key)},
				   expires: at.Token.ExpiredAt,
				   signingCredentials: new  SigningCredentials(new RsaSecurityKey(signKey), SecurityAlgorithms.RsaSha256));
				var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);
				await ss.UpdateKey(sess);
				return Results.Ok(new AuthTokenResponse() { AccessToken = encodedJwt, ExpiresIn = (at.Token.ExpiredAt - DateTime.Now).Seconds, TokenType = "Bearer" });
				
			}
			return Results.StatusCode(401);
		default:
			return Results.BadRequest();
	}

});

app.MapGet(prefix + "/token/validate", () => {
	return Results.Ok(new ValidationInfo() { PublicKey = Convert.ToBase64String(signKey.ExportRSAPublicKey()), Algorithm = signKey.SignatureAlgorithm });
});

app.MapPost(prefix + "/token/invalidate", async (AuthTokenAction tReq, [FromServices] AccessTokenStore ats) => {
	var token = await ats.GetByAccessToken(tReq.Token);

	if (token != null && Convert.ToBase64String(token.Session.App.SecretKey) == tReq.Secret) {
		await ats.RemoveAsync(token);
		return Results.Ok();
	}
	return Results.BadRequest();
});

app.MapPost(prefix + "/token/validate", async (AuthTokenAction tReq, [FromServices] AccessTokenStore ats) => {
	var token = await ats.GetByAccessToken(tReq.Token);

	if (token != null) {
		if (Convert.ToBase64String(token.Session.App.SecretKey) != tReq.Secret) {
			return Results.BadRequest();
		}
		if (token.Token.ExpiredAt > DateTime.Now) {
			return Results.Ok();
		}
	}
	return Results.Unauthorized();
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
