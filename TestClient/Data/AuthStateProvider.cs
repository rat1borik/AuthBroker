using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using BlazorBootstrap;
using System.IdentityModel.Tokens.Jwt;

namespace TestClient.Authentication;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider {

	private readonly ILocalStorageService _localStorage;
	private JwtSecurityTokenHandler JwtHandler;
	TokenValidator _tokenValidator { get; set; }

	public CustomAuthenticationStateProvider(ILocalStorageService localStorage, TokenValidator tokenValidator) {
        _localStorage = localStorage;
		 JwtHandler = new JwtSecurityTokenHandler();
		_tokenValidator	= tokenValidator;
	}

	public override async Task<AuthenticationState> GetAuthenticationStateAsync() {
		try {
			//await Task.Delay(5000);
			var userSessionStorageResult = await _localStorage.GetItemAsync<string>("AuthToken");
			if (userSessionStorageResult != null) {
				SecurityToken securityToken;
				var claims = JwtHandler.ValidateToken(userSessionStorageResult, await _tokenValidator.GetValidationParameters(), out securityToken);
				if (securityToken != null && claims != null)
					return await Task.FromResult(new AuthenticationState(claims));
			}
		} catch {

			NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Claims.Anonymous)));
			return await Task.FromResult(new AuthenticationState(Claims.Anonymous));
		}

		NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Claims.Anonymous)));
		return await Task.FromResult(new AuthenticationState(Claims.Anonymous));

	}

	public async Task UpdateAuthenticationState(string jwtToken = null) {
		ClaimsPrincipal claimsPrincipal = Claims.Anonymous;

		if (jwtToken != null) {
			SecurityToken securityToken;
			var claims = JwtHandler.ValidateToken(jwtToken, await _tokenValidator.GetValidationParameters(), out securityToken);
			if (securityToken != null && claims != null) {
				claimsPrincipal = claims;
				await _localStorage.SetItemAsync("AuthToken", jwtToken);
			}
		} else {
			await _localStorage.RemoveItemAsync("AuthToken");
		}

		NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
	}
	
}
public class TokenValidator {

	public class ValidationInfo {
		public string PublicKey { get; set; }
		public string Algorithm { get; set; }
	}

	IHttpClientFactory ClientFactory { get; set; }

	IConfiguration Cfg { get; set; }
	public TokenValidator(IHttpClientFactory clientFactory, IConfiguration cfg) {
		ClientFactory = clientFactory;
		Cfg = cfg;
	}

	public async Task<TokenValidationParameters> GetValidationParameters() {
		var request = new HttpRequestMessage(HttpMethod.Get,
					"https://localhost:7276/api/v1/token/validate");
		request.Headers.Add("Accept", "application/json");
		request.Headers.Add("User-Agent", "TestApp");
		var client = ClientFactory.CreateClient();
		var response = await client.SendAsync(request);
		if (response.IsSuccessStatusCode && response.Content != null) {
			var result = await response.Content.ReadFromJsonAsync<ValidationInfo>();

			RSA key = RSA.Create();
			int cnt;
			key.ImportRSAPublicKey(Convert.FromBase64String(result.PublicKey), out cnt);
			if (cnt > 0) {
				return new TokenValidationParameters() {
					ValidateLifetime = true,
					ValidateAudience = true,
					ValidateIssuer = true,
					ValidIssuer = "https://localhost:7276",
					ValidAudience = Cfg.GetSection("SSO")["ClientId"],
					IssuerSigningKey = new RsaSecurityKey(key)
				};
			}

		}
		return null;
	}
}

public class AuthTokenAction {

	public string Secret { get; set; }

	public string Token { get; set; }

}

public static class Claims {
	public static ClaimsPrincipal Anonymous = new ClaimsPrincipal(new ClaimsIdentity());
	public static bool IsAnonymous(this AuthenticationState asp) {
		return asp.User == Anonymous;
	}
}