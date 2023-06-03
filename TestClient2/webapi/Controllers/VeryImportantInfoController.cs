using AuthBrokerClient;
using Microsoft.AspNetCore.Mvc;

namespace webapi.Controllers {

	[ApiController]
	[Route("api/v1/account")]
	public class VeryImportantInfoController : ControllerBase {
		private readonly AuthTokenProvider _atp;

		public VeryImportantInfoController(AuthTokenProvider atp) {
			_atp = atp;
		}

		[HttpPost]
		public async Task<IActionResult> GetData(string accessToken) {
			var asp = await _atp.Validate(accessToken);
			if (asp.IsAnonymous()) {
				return Unauthorized();
			}

			var res = new { ClientName = asp.User.Identity.Name, Email = asp.User.Claims.Where(cl => cl.Type == "e-mail").FirstOrDefault().Value, Total = "25 000$" };
			return Ok(res);
		}
	}
}
