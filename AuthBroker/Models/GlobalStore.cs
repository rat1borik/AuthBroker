using Bogus;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace AuthBroker.Model;

public class User {

	[Key]
	public Guid Id { get; set; }
	public string Login { get; set; }
	public string Password { get; set; }
	public JsonDocument? Credentials { get; set; }
}

public class AppGrants {

	[Key]
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public Guid AppId { get; set; }
	public Guid[]? Grants { get; set; }
}

public class Grant {
	[Key]
	public Guid Id { get; set; }
	public string Name { get; set; }
}

public class AppClient {
	[Key]
	public Guid Id { get; set; }
	public byte[] SecretKey { get; set; }
	public string Name { get; set; }
	public ulong AppId { get; set; } = (ulong)Math.Abs(Random.Shared.NextInt64());


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