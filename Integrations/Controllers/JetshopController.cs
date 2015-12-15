using System.Web.Http;
using Integrations.Services;

namespace Integrations.Controllers
{
    [RoutePrefix("Jetshop")]
    public class JetshopController : ApiController
    {
        [Route("Products")]
        [HttpGet]
        public IHttpActionResult GetProducts([FromUri] string domain, [FromUri] int top = 0)
        {
            var service = new JetshopService();
            var url = $"http://{domain}/Services/Rest/v1/json/products";
            var result = service.GetProducts(url, top);
            return Ok(result);
        }
    }
}
