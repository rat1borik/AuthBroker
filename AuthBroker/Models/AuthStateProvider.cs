using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;
using System.Security.Claims;
using AuthBroker.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Text;

namespace AuthBroker.Authentication;

public class UserSession {
	public string UserName { get; set; }
	public string Password { get; set; }
}

public class CustomAuthenticationStateProvider : AuthenticationStateProvider {
	private readonly ILocalStorageService _localStorage;
	private readonly CryptoProvider _cryptoProvider;
	private readonly UserAccStore _userStore;
	private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

	public CustomAuthenticationStateProvider(ILocalStorageService localStorage, CryptoProvider cryptoProvider, UserAccStore userStore) {
        _localStorage = localStorage;
		_cryptoProvider = cryptoProvider;
		_userStore = userStore;
	}

	public override async Task<AuthenticationState> GetAuthenticationStateAsync() {
		try {
			//await Task.Delay(5000);
			var userSessionStorageResult = await _localStorage.GetItemAsync<string>("session");
			if (userSessionStorageResult == null)
				return await Task.FromResult(new AuthenticationState(_anonymous));
			var data = _cryptoProvider.Decrypt<UserSession>(userSessionStorageResult);
			if (data == null)
				return await Task.FromResult(new AuthenticationState(_anonymous));
			var user = await _userStore.GetByLogin(data.UserName);
			if (user == null || !user.VerifyPassword(Encoding.UTF8.GetBytes(data.Password)))
				return await Task.FromResult(new AuthenticationState(_anonymous));
			var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
			{
				new Claim(ClaimTypes.Name, user.Login),
				new Claim(ClaimTypes.Role, user.IsAdmin?"Admin":"User")
			}, "Auth"));
			return await Task.FromResult(new AuthenticationState(claimsPrincipal));
		} catch {
			return await Task.FromResult(new AuthenticationState(_anonymous));
		}
	}

	public async Task UpdateAuthenticationState(UserSession userSession) {
		if (userSession != null) {
			var user = await _userStore.GetByLogin(userSession.UserName);

			var s = Encoding.UTF8.GetBytes(userSession.Password);
			if (user != null && user.VerifyPassword(s)) {
				var data = _cryptoProvider.Encrypt<UserSession>(userSession);
				if (data != null) {
					await _localStorage.SetItemAsync("session", data);
					var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
					{
						new Claim(ClaimTypes.Name, user.Login),
						new Claim(ClaimTypes.Role, user.IsAdmin?"Admin":"User")
					}, "Auth"));
					NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
					return;
				}
			}
			
		}

		await _localStorage.RemoveItemAsync("session");
		NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
	}
}
