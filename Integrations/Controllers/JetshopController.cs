using System.Net;
using Newtonsoft.Json;
using System.Web.Http;

namespace Integrations.Controllers
{
    [RoutePrefix("Jetshop")]
    public class JetshopController : ApiController
    {
        [Route("Products")]
        [HttpGet]
        public IHttpActionResult GetProducts([FromUri] string domain)
        {
            var url = $"http://{domain}/Services/Rest/v1/json/products";
            string jsonString;
            using (var wc = new WebClient())
            {

                jsonString = wc.DownloadString(url);
            }
            dynamic cleanJson = JsonConvert.DeserializeObject(jsonString);
            return Ok(cleanJson);
        }
    }
}
