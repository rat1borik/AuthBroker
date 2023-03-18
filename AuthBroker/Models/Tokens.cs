using System.Text.Json;

namespace AuthBroker.Models;
public class AuthTokenRequest {
	public string GrantType { get; set; }

	public string Code { get; set; }

	public string Secret { get; set; }
}
public class AuthTokenResponse {
	public string AccessToken { get; set; }
	public int ExpiresIn { get; set; }
	public string TokenType { get; set; }
}

public class ValidationInfo {
	public string PublicKey { get; set; }
	public string Algorithm { get; set; }
}


public class SnakeCaseNamingPolicy : JsonNamingPolicy {
	public static SnakeCaseNamingPolicy Instance { get; } = new SnakeCaseNamingPolicy();

	public override string ConvertName(string name) {
		// Conversion to other naming convention goes here. Like SnakeCase, KebabCase etc.
		return name.ToSnakeCase();
	}
}

public static class StringUtils {
	public static string ToSnakeCase(this string str) {
		return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
	}
}