using Microsoft.AspNetCore.Mvc;

namespace SearchAPI.Controllers
{
    [ApiController]
    [Route("")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Content("<link rel=\"stylesheet\" type=\"text/css\" link=\"..Css/styles.css\">" +
                      "<div class=\"container\">" +
                      "<h2>Welcome to Product Management System</h2>" +
                      "<p>You can navigate to:</p>" +
                      "<ul>" +
                      "<li><a href=\"/products\">Product Grid</a></li>" +
                      "<li><a href=\"/swagger/index.html\">API Documentation</a></li>" +
                      "</ul>" +
                      "</div>", "text/html");
        }
    }
}
