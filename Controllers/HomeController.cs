using Microsoft.AspNetCore.Mvc;

namespace RY.TransferImagePro.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class HomeController : ControllerBase
    {
        public ActionResult Index()
        {
            var data = new
            {
            };
            return Ok(data);
        }
    }
}