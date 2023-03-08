using System.Security.Cryptography;
using System.Text.Json;
using System.Linq;

namespace TestClient.Data {
	public class StateProvider {

		Aes instance;
		readonly TimeSpan lifetime = TimeSpan.FromSeconds(60);

		public StateProvider() {
			instance = Aes.Create();

			instance.KeySize = 256;
			instance.BlockSize = 128;
			//ivSize = 16

			instance.GenerateKey();
		}

		public string GetState() {
			var stateData = new StateData() { ExpiredIn = DateTime. Now + lifetime, Salt = Guid.NewGuid().ToString()};
			instance.GenerateIV();

			return Convert.ToBase64String(instance.IV.Concat(instance.EncryptCfb(JsonSerializer.SerializeToUtf8Bytes(stateData), instance.IV, PaddingMode.PKCS7)).ToArray());
		}

		public bool ValidateState(string state) {
			byte[] body;

			try {
				body = Convert.FromBase64String(state);
			} catch (FormatException) {
				return false;
			}

			if (body.Length <= instance.BlockSize / 8) {
				return false;
			}

			var iv = body.Take(instance.BlockSize / 8).ToArray();
			var stateContent = body.Skip(instance.BlockSize / 8).ToArray();

			try {
				var res = instance.DecryptCfb(stateContent, iv, PaddingMode.PKCS7);
				if (res != null) {
					var resObj = JsonSerializer.Deserialize<StateData>(res);
					if (resObj != null && resObj.Salt.IsGuid() && resObj?.ExpiredIn > DateTime.Now) {
						return true;
					}
				}
			} catch (CryptographicException) {
				return false;
			}

			return false;

		}

	}

	public class StateData {
		public string Salt { get; set; }
		public DateTime ExpiredIn { get; set; }
	}
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