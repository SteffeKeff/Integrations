using System;
using ShopifyAPIAdapterLibrary;
using Newtonsoft.Json.Linq;

namespace Integrations.Services
{
    public class ShopifyService
    {
        private const string API_KEY = "eea19e5efc66ab8c6abe9161e75e58f7";
        private const string API_SECRET = "80156e9299fe2a816dfe57a6619549d0";
        private const string CALLBACK_URL = "http://localhost:1337/Shopify/callback";
        private readonly string[] rights = new string[] { "read_products", "read_customers" };
        ShopifyAPIAuthorizer authorizer;

        public string GetLoginUrl(string shopName)
        {
            Uri returnUrl = new Uri(CALLBACK_URL);
            authorizer = new ShopifyAPIAuthorizer(shopName, API_KEY, API_SECRET);
            var authUrl = authorizer.GetAuthorizationURL(rights, returnUrl.ToString());
            return authUrl;
        }

        public dynamic GetAccessToken(string code, string shopName)
        {
            authorizer = new ShopifyAPIAuthorizer(shopName, API_KEY, API_SECRET);
            ShopifyAuthorizationState authState = authorizer.AuthorizeClient(code);

            return authState?.AccessToken;
        }

        public dynamic GetEntities(string entity, string token, string shopName, int top, string[] fields)
        {
            ShopifyAuthorizationState authState = new ShopifyAuthorizationState
            {
                AccessToken = token,
                ShopName = shopName
            };
            var api = new ShopifyAPIClient(authState, new JsonDataTranslator());

            string fieldQuery = "";
            JArray summary = new JArray();

            if (fields.Length != 0)
            {
                var commaSeparatedFields = string.Join(",", fields);
                fieldQuery = "fields=" + commaSeparatedFields + "&";
            }

            if (top == 0 || top > 250)
            {
                dynamic count = api.Get($"/admin/{entity}/count.json");

                JObject json = count;
                double realCount = int.Parse(json.GetValue("count").ToString());
                realCount = realCount / 250;
                int loops = (int)Math.Ceiling(realCount);

                for (int i = 1; i <= loops; i++)
                {
                    collectEntities(api, summary, entity, 250, i, fieldQuery);
                }
            }
            else
            {
                collectEntities(api, summary, entity, top, 1, fieldQuery);
            }
            return summary;
        }

        public dynamic GetEntity(string entity, string id, string token, string shopName, string[] fields)
        {
            ShopifyAuthorizationState authState = new ShopifyAuthorizationState
            {
                AccessToken = token,
                ShopName = shopName
            };
            var api = new ShopifyAPIClient(authState, new JsonDataTranslator());

            var queryString = "";
            if (fields.Length != 0)
            {
                var commaSeparatedFields = string.Join(",", fields);
                queryString = "fields=" + commaSeparatedFields + "&";
            }

            string urlSuffix;
            if (entity.Equals("collections"))
            {
                entity = "products";
                queryString = queryString + "collection_id=" + id;
                urlSuffix = $"/admin/{entity}.json?{queryString}";

            }
            else
            {
                urlSuffix = $"/admin/{entity}/{id}.json?{queryString}";
            }

            JObject entityObject = (JObject)api.Get(urlSuffix);

            return entityObject;
        }

        private void collectEntities(ShopifyAPIClient api, JArray summary, string entity, int top, int page, string fieldQuery)
        {
            JObject entityObject = (JObject)api.Get($"/admin/{entity}.json?{fieldQuery}limit={top}&page={page}");
            JToken entitiesObject = entityObject.GetValue($"{entity}");
            foreach (var jToken in entitiesObject)
            {
                var entityObj = (JObject)jToken;
                summary.Add(entityObj);
            }
        }

        public dynamic GetCustomerSavedSearches(string token, string shopName)
        {
            JArray summary = new JArray();
            ShopifyAuthorizationState authState = new ShopifyAuthorizationState
            {
                AccessToken = token,
                ShopName = shopName
            };
            var api = new ShopifyAPIClient(authState, new JsonDataTranslator());

            JObject filterObject = (JObject)api.Get("/admin/customer_saved_searches.json");
            JToken filtersObject = filterObject.GetValue("customer_saved_searches");
            foreach (var jToken in filtersObject)
            {
                var filter = (JObject)jToken;
                summary.Add(filter);
            }

            return summary;
        }

        internal object GetCustomerSavedSearch(string token, string shopName, int id, string[] fields)
        {
            JArray customers = new JArray();
            ShopifyAuthorizationState authState = new ShopifyAuthorizationState
            {
                AccessToken = token,
                ShopName = shopName
            };
            var api = new ShopifyAPIClient(authState, new JsonDataTranslator());
            string fieldQuery = "";

            if (fields.Length != 0)
            {
                var commaSeparatedFields = string.Join(",", fields);
                fieldQuery = "?fields=" + commaSeparatedFields;
            }

            JObject filterObject = (JObject)api.Get($"/admin/customer_saved_searches/{id}/customers.json{fieldQuery}");
            JToken customersObject = filterObject.GetValue("customers");
            foreach (var jToken in customersObject)
            {
                var customer = (JObject)jToken;
                customers.Add(customer);
            }

            return customers;
        }
    }
}