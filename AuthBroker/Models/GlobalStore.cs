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

namespace AuthBroker.Models;

public class User {

	[Key]
	public Guid Id { get; set; }
	public string Login { get; set; }
	public byte[] Password { get; set; }

	public bool IsAdmin { get; set; }
	[Column(TypeName = "jsonb")]
	public Credentials? Credentials { get; set; }
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