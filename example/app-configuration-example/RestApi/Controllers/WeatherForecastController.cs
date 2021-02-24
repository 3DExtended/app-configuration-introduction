using Microsoft.AspNetCore.Mvc;

namespace RestApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SettingsController : ControllerBase
    {
        public SettingsController()
        {
        }

        [HttpGet]
        public string Get()
        {
            return "asdf";
        }
    }
}