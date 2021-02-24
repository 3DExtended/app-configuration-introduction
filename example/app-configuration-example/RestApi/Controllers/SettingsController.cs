using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace RestApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public SettingsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public string Get()
        {
            return _configuration["SomeRandomConfiguration"];
        }
    }
}