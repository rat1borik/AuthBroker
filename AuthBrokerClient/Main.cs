using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;

namespace AuthBrokerClient {
	class TokenValidator {

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
						string.Format("{0}/api/v1/token/validate", Cfg.GetSection("SSO")["Host"]));
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
						ValidIssuer = Cfg.GetSection("SSO")["Host"],
						ValidAudience = Cfg.GetSection("SSO")["ClientId"],
						IssuerSigningKey = new RsaSecurityKey(key)
					};
				}

			}
			return null;
		}
	}
	public class AuthTokenProvider {

		TokenValidator _tokenValidator;
		IConfiguration _cfg;
		IHttpClientFactory _httpClientFactory;
		JwtSecurityTokenHandler JwtHandler = new();

		public AuthTokenProvider(IHttpClientFactory httpClientFactory, IConfiguration cfg) {
			_tokenValidator = new(httpClientFactory, cfg);
			_httpClientFactory = httpClientFactory;
			_cfg = cfg;
		}

		public async Task<AuthenticationState> Validate(string token, bool expCheck = true) {
			SecurityToken securityToken;
			try {
				var claims = JwtHandler.ValidateToken(token, await _tokenValidator.GetValidationParameters(), out securityToken);
				if (securityToken != null && claims != null) {
					if (expCheck) {
						var request = new HttpRequestMessage(HttpMethod.Post,
							string.Format("{0}/api/v1/token/validate", _cfg.GetSection("SSO")["Host"]));
						request.Headers.Add("Accept", "application/json");
						request.Headers.Add("User-Agent", "TestApp");
						request.Content = JsonContent.Create(new AuthTokenAction { Token = claims.Claims.Where(cl => cl.Type == "access_token").FirstOrDefault().Value, Secret = _cfg.GetSection("SSO")["ClientSecret"] });

						var client = _httpClientFactory.CreateClient();

						var response = await client.SendAsync(request);

						if (response.IsSuccessStatusCode) {
							return await Task.FromResult(new AuthenticationState(claims));
						}
					} else {
						return await Task.FromResult(new AuthenticationState(claims));
					}
				}
			} catch {
				return await Task.FromResult(new AuthenticationState(Claims.Anonymous));
			}
			return await Task.FromResult(new AuthenticationState(Claims.Anonymous));
		}

		public async Task<bool> Invalidate(string token) {
			var request = new HttpRequestMessage(HttpMethod.Post,
					_cfg.GetSection("SSO")["Host"] + "/api/v1/token/invalidate");
			request.Headers.Add("Accept", "application/json");
			request.Headers.Add("User-Agent", "TestApp");
			request.Content = JsonContent.Create(new AuthTokenAction {
				Token = token,
				Secret = _cfg.GetSection("SSO")["ClientSecret"]
			});

			var client = _httpClientFactory.CreateClient();

			var response = await client.SendAsync(request);
			if (!response.IsSuccessStatusCode) {
				return false;
			}
			return true;
		}

		public async Task<(string? err, string? token)> Authenticate(string code, string remoteIp, string userAgent) {
			var request = new HttpRequestMessage(HttpMethod.Post,
					_cfg.GetSection("SSO")["Host"] + "/api/v1/token");
			request.Headers.Add("Accept", "application/json");
			request.Headers.Add("User-Agent", "TestApp");
			request.Content = JsonContent.Create(new AuthTokenRequest { GrantType = "authorization_code", Code = code, Secret = _cfg.GetSection("SSO")["ClientSecret"], RemoteIp = remoteIp, UserAgent = userAgent });

			var client = _httpClientFactory.CreateClient();

			var response = await client.SendAsync(request);

			if (response.IsSuccessStatusCode && response.Content != null) {
				var result = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
				var jwtHandler = new JwtSecurityTokenHandler();
				SecurityToken securityToken;
				try {
					var claims = jwtHandler.ValidateToken(result.AccessToken, await _tokenValidator.GetValidationParameters(), out securityToken);
					if (securityToken != null && claims != null) {
						return await Task.FromResult(((string?)null, result.AccessToken));
					}
				} catch (SecurityTokenException) {
					return await Task.FromResult(("Incorrect AuthData",(string?)null));
				} catch (ArgumentNullException) {
					return await Task.FromResult(("Incorrect AuthData", (string?)null));
				}
			} else {
				return await Task.FromResult((response.ReasonPhrase, (string?)null));
			}

			return await Task.FromResult(("Authentication error", (string?)null));
		}

		public string GetAuthenticationURL(string redirectUrl, string state) =>
			string.Format(_cfg.GetSection("SSO")["Host"] + "/auth?app_id={0}&response_type=code&scopes=e-mail&redirect_uri={1}?state={2}", _cfg.GetSection("SSO")["ClientId"], redirectUrl, state);
	}

	public static class Claims {
		public static ClaimsPrincipal Anonymous = new ClaimsPrincipal(new ClaimsIdentity());
		public static bool IsAnonymous(this AuthenticationState asp) {
			return asp.User == Anonymous;
		}
	}
	public class AuthTokenAction {

		public string Secret { get; set; }

		public string Token { get; set; }

	}

	public class AuthTokenRequest {
		public string GrantType { get; set; }

		public string Code { get; set; }

		public string Secret { get; set; }

		public string RemoteIp { get; set; }
		public string UserAgent { get; set; }
	}

	public class AuthTokenResponse {
		public string AccessToken { get; set; }
		public int ExpiresIn { get; set; }
		public string TokenType { get; set; }
	}
	public static class GuidEx {
		public static bool IsGuid(this string value) {
			Guid x;
			return Guid.TryParse(value, out x);
		}
	}

	public static class Base64Ex {
		public static string Base64UrlEncode(this string value) {
			return value.Replace('+', '.').Replace('/', '_').Replace('=', '-');
		}
		public static string Base64UrlDecode(this string value) {
			return value.Replace('.', '+').Replace('_', '/').Replace('-', '=');
		}
	}
}

