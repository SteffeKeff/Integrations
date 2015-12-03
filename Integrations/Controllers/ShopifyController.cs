using Integrations.Services;
using System.Web.Http;

namespace Integrations.Controllers
{
    [RoutePrefix("Shopify")]
    public class ShopifyController : ApiController
    {
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
            var shopName = shop.Replace(".myshopify.com", "");
            var service = new ShopifyService();
            var token = service.GetAccessToken(code, shopName);

            return Ok(token);   // Returns 401 if our App is'nt installed or the Token is invalid
        }

        [Route("Customers")]
        [HttpGet]
        public IHttpActionResult GetCustomers([FromUri] string token, [FromUri] string shopname, [FromUri] string[] fields, [FromUri] int top = 0)
        {
            var service = new ShopifyService();
            var customers = service.GetEntities("customers", token, shopname, top, fields);

            return Ok(customers);
        }

        [Route("Products")]
        [HttpGet]
        public IHttpActionResult GetProducts([FromUri] string token, [FromUri] string shopname, [FromUri] string[] fields, [FromUri] int top = 0)
        {
            var service = new ShopifyService();
            var products = service.GetEntities("products", token, shopname, top, fields);

            return Ok(products);
        }

        [Route("Collections")]
        [HttpGet]
        public IHttpActionResult GetCollections([FromUri] string token, [FromUri] string shopname, [FromUri] string[] fields, [FromUri] int top = 0)
        {
            var service = new ShopifyService();
            var collections = service.GetEntities("custom_collections", token, shopname, top, fields);

            return Ok(collections);
        }

        [Route("Customers/{id}")]
        [HttpGet]
        public IHttpActionResult GetCustomer(string id, [FromUri] string token, [FromUri] string shopname, [FromUri] string[] fields)
        {
            var service = new ShopifyService();
            var entities = service.GetEntity("customers", id, token, shopname, fields);

            return Ok(entities);
        }

        [Route("Products/{id}")]
        [HttpGet]
        public IHttpActionResult GetProduct(string id, [FromUri] string token, [FromUri] string shopname, [FromUri] string[] fields)
        {
            var service = new ShopifyService();
            var entities = service.GetEntity("products", id, token, shopname, fields);

            return Ok(entities);
        }

        [Route("Collections/{id}")]
        [HttpGet]
        public IHttpActionResult GetCollection(string id, [FromUri] string token, [FromUri] string shopname, [FromUri] string[] fields)
        {
            var service = new ShopifyService();
            var entities = service.GetEntity("collections", id, token, shopname, fields);

            return Ok(entities);
        }

        [Route("Filters")]
        [HttpGet]
        public IHttpActionResult GetFilters([FromUri] string token, [FromUri] string shopname)
        {
            var service = new ShopifyService();
            var filterArray = service.GetCustomerSavedSearches(token, shopname);

            return Ok(filterArray);
        }

        [Route("Filters/{id}")]
        [HttpGet]
        public IHttpActionResult GetFilter(int id, [FromUri] string token, [FromUri] string shopname, [FromUri] string[] fields)
        {
            var service = new ShopifyService();
            var filter = service.GetCustomerSavedSearch(token, shopname, id, fields);

            return Ok(filter);
        }
    }
}