using System.Security.Cryptography;
using System.Text.Json;

namespace AuthBroker.Models {
	public class CryptoProvider{

		public Aes instance;
		public CryptoProvider() { 
			instance = Aes.Create();
			instance.KeySize = 256;
			instance.BlockSize = 128;
			instance.GenerateKey();
		}

		public string Encrypt<T>(T data) {
			instance.GenerateIV();
			return Convert.ToBase64String(instance.IV.Concat(instance.EncryptCfb(JsonSerializer.SerializeToUtf8Bytes<T>(data), instance.IV, PaddingMode.PKCS7)).ToArray());
		}

		public T? Decrypt<T>(string data) {
			var body = Convert.FromBase64String(data);

			if (body.Length <= instance.BlockSize / 8) {
				return default(T);
			}

			var iv = body.Take(instance.BlockSize / 8).ToArray();
			var stateContent = body.Skip(instance.BlockSize / 8).ToArray();

			var res = instance.DecryptCfb(stateContent, iv, PaddingMode.PKCS7);
			if (res != null) {
				return JsonSerializer.Deserialize<T>(res);
			}

			return default(T);
		}
	}
}
