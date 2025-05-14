using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ShareVault.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : Controller
    {
        [HttpGet("secure")]
        [Authorize]
        public IActionResult SecureEndpoint()
        {
            return Ok("Bu mesajı yalnızca giriş yapan kullanıcılar görebilir.");
        }

        [HttpGet("public")]
        public IActionResult PublicEndpoint()
        {
            return Ok("Bu mesajı herkes görebilir.");
        }
    }
}
