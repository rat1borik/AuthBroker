using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using BlazorBootstrap;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using AuthBrokerClient;

namespace TestClient.Authentication;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider {

	private readonly ILocalStorageService _localStorage;
	AuthTokenProvider _atv;



	public CustomAuthenticationStateProvider(ILocalStorageService localStorage, AuthTokenProvider atv) {
		_localStorage = localStorage;
		_atv = atv;
	}

	public override async Task<AuthenticationState> GetAuthenticationStateAsync() {
		try {

			var userSessionStorageResult = await _localStorage.GetItemAsync<string>("AuthToken");
			if (userSessionStorageResult != null) {
				var asp = await _atv.Validate(userSessionStorageResult);
				if (!asp.IsAnonymous()) {
					return asp;
				}
			}
		} catch { }

		NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Claims.Anonymous)));
		return await Task.FromResult(new AuthenticationState(Claims.Anonymous));

	}

	public async Task UpdateAuthenticationState(string jwtToken = null) {
		var asp = new AuthenticationState(Claims.Anonymous);
		if (jwtToken != null) {
			asp = await _atv.Validate(jwtToken);
			if (!asp.IsAnonymous()) {
				await _localStorage.SetItemAsync("AuthToken", jwtToken);
			}
		} else {
			await _localStorage.RemoveItemAsync("AuthToken");
		}

		NotifyAuthenticationStateChanged(Task.FromResult(asp));
	}
	
}