using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;
using System.Security.Claims;

namespace AuthBroker.Authentication;

public class UserSession {
	public string UserName { get; set; }
	public string Role { get; set; }
}

public class CustomAuthenticationStateProvider : AuthenticationStateProvider {
	private readonly ILocalStorageService _localStorage;
	private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

	public CustomAuthenticationStateProvider(ILocalStorageService localStorage) {
        _localStorage = localStorage;
	}

	public override async Task<AuthenticationState> GetAuthenticationStateAsync() {
		try {
			//await Task.Delay(5000);
			var userSessionStorageResult = await _localStorage.GetItemAsync<UserSession>("UserSession");
			if (userSessionStorageResult == null)
				return await Task.FromResult(new AuthenticationState(_anonymous));
			var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
			{
					new Claim(ClaimTypes.Name, userSessionStorageResult.UserName),
					new Claim(ClaimTypes.Role, userSessionStorageResult.Role)
				}, "CustomAuth"));
			return await Task.FromResult(new AuthenticationState(claimsPrincipal));
		} catch {
			return await Task.FromResult(new AuthenticationState(_anonymous));
		}
	}

	public async Task UpdateAuthenticationState(UserSession userSession) {
		ClaimsPrincipal claimsPrincipal;

		if (userSession != null) {
			await _localStorage.SetItemAsync("UserSession", userSession);
			claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
			{
					new Claim(ClaimTypes.Name, userSession.UserName),
					new Claim(ClaimTypes.Role, userSession.Role)
				}));
		} else {
			await _localStorage.RemoveItemAsync("UserSession");
			claimsPrincipal = _anonymous;
		}

		NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
	}
}
