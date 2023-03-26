using Bogus;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Data;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using BlazorBootstrap.Extensions;
using System.Collections;

namespace AuthBroker.Models;
static class StructuralExtensions {
	public static bool SEquals<T>(this T a, T b)
		where T : IStructuralEquatable {
		return a.Equals(b, StructuralComparisons.StructuralEqualityComparer);
	}
}
public class User {

	[Key]
	public Guid Id { get; set; }
	public string Login { get; set; }

	private byte[] password;
	public byte[] Password {
		get { 
			return password;
		} set {
			this.password = SHA256.Create().ComputeHash(value);
		} }

	public bool IsAdmin { get; set; }
	[Column(TypeName = "jsonb")]
	public Credentials? Credentials { get; set; }

	public bool VerifyPassword(byte[] password) {
		var s = SHA256.Create().ComputeHash(password);
		return s.SEquals(Password);
	}
}
public class Credentials {
	public string Email { get; set; }
}
public class Session {

	[Key]
	public Guid Id { get; set; }
	public string Code { get; set; }
	public User User { get; set; }
	public AppClient App { get; set; }
	public ICollection<Scope>? Scopes { get; set; }
}
public class AccessToken {

	[Key]
	public Guid Id { get; set; }
	public Session Session { get; set; }

	[Column(TypeName = "jsonb")]
	public Token Token { get; set; }

	[Column(TypeName = "jsonb")]
	public Token RefreshToken { get; set; }
	public IPAddress Ip { get; set; }

	public AccessToken() {
		Token = new Token { ExpiredAt = DateTime.Now + TimeSpan.FromHours(2), Key = RandomTokenGenerator.CreateKey() };
		RefreshToken = new Token { ExpiredAt = DateTime.Now + TimeSpan.FromHours(24), Key = RandomTokenGenerator.CreateKey() };
	}
}

public class Token {
	public string Key { get; set; }
	public DateTime ExpiredAt { get; set; }
}

public class Scope {
	[Key]
	public Guid Id { get; set; }
	public string Name { get; set; }
	public ICollection<Session>? Sessions { get; set; }
}

public class AppClient {
	public string Id { get; set; } = Math.Abs(Random.Shared.NextInt64()).ToString();
	public byte[] SecretKey { get; set; }
	public string Name { get; set; }

    public Uri[] AllowedRedirectUris { get; set; }


    public AppClient() {
        PrepareKey();
    }

	public void PrepareKey() {

        using (Aes keygen = Aes.Create()) {
            keygen.KeySize = 256;
            keygen.GenerateKey();
			this.SecretKey = keygen.Key;
		}
	}

	public string GetSecretKey() {
		return Convert.ToBase64String(this.SecretKey);
	}
}