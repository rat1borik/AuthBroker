using AuthBrokerClient;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace webapi.Controllers {

	[ApiController]
	[Route("api/v1/auth")]
	public class AuthController : ControllerBase {

		public class AuthRequest {
			public string AuthCode { get; set; }	
		}

		private readonly AuthTokenProvider _atp;

		public AuthController(AuthTokenProvider atp) {
			_atp = atp;
		}

		[HttpGet("url")]
		public object GetUrl() {
			return new { Url = _atp.GetAuthenticationURL("https://localhost:5003/auth", null) };
		}

		[HttpPost]
		public async Task<IActionResult> Auth(AuthRequest req) {
			var (err, token) = await _atp.Authenticate(req.AuthCode, HttpContext.Connection.RemoteIpAddress.ToString(), HttpContext.Request.Headers["User-Agent"]);
			if (err != null) {
				return BadRequest();
			}
			return Ok(new { AuthToken = token });
		}
	}
}
