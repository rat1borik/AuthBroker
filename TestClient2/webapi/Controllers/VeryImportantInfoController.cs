using AuthBrokerClient;
using Microsoft.AspNetCore.Mvc;

namespace webapi.Controllers {

	[ApiController]
	[Route("api/v1")]
	public class VeryImportantInfoController : ControllerBase {
		public class VIIRequest {
			public string AccToken { get; set; }
		}

		private readonly AuthTokenProvider _atp;

		public VeryImportantInfoController(AuthTokenProvider atp) {
			_atp = atp;
		}

		[HttpPost("account")]
		public async Task<IActionResult> GetAccount(VIIRequest req) {
			var asp = await _atp.Validate(req.AccToken);
			if (asp.IsAnonymous()) {
				return Unauthorized();
			}

			var res = new { ClientName = asp.User.Identity.Name, Email = asp.User.Claims.Where(cl => cl.Type == "e-mail").FirstOrDefault().Value, Total = Random.Shared.Next(100, 10000).ToString() + " $" };
			return Ok(res);
		}
	}
}
