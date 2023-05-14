using System.Security.Cryptography;
using System.Text.Json;
using System.Linq;
using Microsoft.Extensions.Configuration;
using AuthBrokerClient;

namespace TestClient.Data {
	public class StateProvider {

		Aes instance;
		IConfiguration _cfg;
		readonly TimeSpan lifetime;

		public StateProvider(IConfiguration cfg) {
			_cfg = cfg;
			instance = Aes.Create();

			instance.KeySize = 256;
			instance.BlockSize = 128;
			//ivSize = 16

			instance.GenerateKey();

			lifetime = TimeSpan.FromSeconds(double.Parse(_cfg.GetSection("SSO")["StateLifeTime"] ?? "120"));
		}

		public string GetState(string ip) {
			var stateData = new StateData() { ExpiredIn = DateTime.Now + lifetime, Salt = Guid.NewGuid().ToString(), Ip = ip};
			instance.GenerateIV();

			return Convert.ToBase64String(instance.IV.Concat(instance.EncryptCfb(JsonSerializer.SerializeToUtf8Bytes(stateData), instance.IV, PaddingMode.PKCS7)).ToArray());
		}

		public bool ValidateState(string state, string ip) {
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
					return (resObj != null && resObj.Salt.IsGuid() && resObj?.ExpiredIn > DateTime.Now && (resObj.Ip == ip || !(bool.Parse(_cfg.GetSection("SSO")["VerifyIp"] ?? "false"))));
				}
			} catch (CryptographicException) { }

			return false;

		}

	}

	public class StateData {
		public string Salt { get; set; }
		public DateTime ExpiredIn { get; set; }

		public string Ip { get; set; }
	}
}