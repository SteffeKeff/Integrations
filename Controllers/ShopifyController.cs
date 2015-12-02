using Integrations.Services;
using System.Web.Http;

namespace Integrations.Controllers
{
    [RoutePrefix("Shopify")]
    public class ShopifyController : ApiController
    {
        ShopifyService service;

        [Route("Login/{shopname}")]
        [HttpGet]
        public IHttpActionResult Login(string shopname)
        {
            var loginUrl = new ShopifyService().GetLoginUrl(shopname);

            return Redirect(loginUrl);
        }

        [Route("Callback")]
        [HttpGet]
        public IHttpActionResult GetAccessToken([FromUri] string code, [FromUri] string shop)
        {
            service = new ShopifyService();
            var shopName = shop.Replace(".myshopify.com", "");
            var token = service.GetAccessToken(code, shopName);

            return Ok(token);   // Returns 401 if our App is'nt installed or the Token is invalid
        }

        [Route("{entity}")]
        [HttpGet]
        public IHttpActionResult GetEntity(string entity, [FromUri] string token, [FromUri] string shopname, [FromUri] string[] fields, [FromUri] int top = 0)
        {
            entity = entity.ToLower();
            if (entity.Equals("customers") || entity.Equals("products"))
            {
                ShopifyService service = new ShopifyService();
                var entities = service.GetEntities(entity, token, shopname, top, fields);
                return Ok(entities);
            }

            return BadRequest();
        }

        [Route("Filters")]
        [HttpGet]
        public IHttpActionResult GetFilters([FromUri] string token, [FromUri] string shopname)
        {
            service = new ShopifyService();
            var filterArray = service.GetCustomerSavedSearches(token, shopname);

            return Ok(filterArray);
        }

        [Route("Filters/{id}")]
        [HttpGet]
        public IHttpActionResult GetCustomersByFilter(int id, [FromUri] string token, [FromUri] string shopname, [FromUri] string[] fields)
        {
            service = new ShopifyService();
            var customers = service.GetCustomerSavedSearch(token, shopname, id, fields);

            return Ok(customers);
        }
    }
}