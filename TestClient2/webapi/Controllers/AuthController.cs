using AuthBrokerClient;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace webapi.Controllers {

	[ApiController]
	[Route("api/v1/auth")]
	public class AuthController : ControllerBase {


		private readonly AuthTokenProvider _atp;

		public AuthController(AuthTokenProvider atp) {
			_atp = atp;
		}

		[HttpGet("url")]
		public object GetUrl() {
			return new { Url = _atp.GetAuthenticationURL("https://"+HttpContext.Request.Host.ToString()+"/auth", null)};
		}

		[HttpPost]
		public async Task<IActionResult> Auth(string authCode) {
			var (err, token) = await _atp.Authenticate(authCode, HttpContext.Connection.RemoteIpAddress.ToString(), HttpContext.Request.Headers["User-Agent"]);
			if (err != null) {
				return BadRequest();
			}
			return Ok(new { AuthToken = token });
		}
	}
}
