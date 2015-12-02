using System;
using ShopifyAPIAdapterLibrary;
using Newtonsoft.Json.Linq;

namespace Integrations.Services
{
    public class ShopifyService
    {
        private string API_KEY = "eea19e5efc66ab8c6abe9161e75e58f7";
        private string API_SECRET = "80156e9299fe2a816dfe57a6619549d0";
        private string CALLBACK_URL = "http://localhost:1337/Shopify/callback";
        private string[] RIGHTS = new string[] { "read_products", "read_customers" };
        ShopifyAPIAuthorizer authorizer;

        public string GetLoginUrl(string shopName)
        {
            var returnURL = new Uri(CALLBACK_URL);
            authorizer = new ShopifyAPIAuthorizer(shopName, API_KEY, API_SECRET);
            var authUrl = authorizer.GetAuthorizationURL(RIGHTS, returnURL.ToString());
            return authUrl;
        }

        public string GetAccessToken(string code, string shopName)
        {
            authorizer = new ShopifyAPIAuthorizer(shopName, API_KEY, API_SECRET);
            var authState = authorizer.AuthorizeClient(code);

            if (authState != null && authState.AccessToken != null)
            {
                var api = new ShopifyAPIClient(authState, new JsonDataTranslator());
                return authState.AccessToken;
            }
            return null;
        }

        public JArray GetEntities(string entity, string token, string shopName, int top, string[] fields)
        {
            var authState = new ShopifyAuthorizationState();
            authState.AccessToken = token;
            authState.ShopName = shopName;
            var api = new ShopifyAPIClient(authState, new JsonDataTranslator());

            dynamic data;
            var fieldQuery = "";
            var summary = new JArray();

            if (fields.Length != 0)
            {
                var commaSeparatedFields = string.Join(",", fields);
                fieldQuery = "fields=" + commaSeparatedFields + "&";
            }

            if (top == 0 || top > 250)
            {
                dynamic count = api.Get(string.Format("/admin/{0}/count.json", entity));

                var json = count;
                data = count;
                double realCount = int.Parse(json.GetValue("count").ToString());
                realCount = realCount / 250;
                var loops = (int)Math.Ceiling(realCount);

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

        private void collectEntities(ShopifyAPIClient api, JArray summary, string entity, int top, int page, string fieldQuery)
        {
            var entityObject = (JObject)api.Get(string.Format("/admin/{0}.json?{1}limit={2}&page={3}", entity, fieldQuery, top, page));
            var entitiesObject = entityObject.GetValue(string.Format("{0}", entity));
            foreach (JObject customer in entitiesObject)
            {
                summary.Add(customer);
            }
        }

        public JArray GetCustomerSavedSearches(string token, string shopName)
        {
            var summary = new JArray();
            var authState = new ShopifyAuthorizationState();
            authState.AccessToken = token;
            authState.ShopName = shopName;
            var api = new ShopifyAPIClient(authState, new JsonDataTranslator());

            var filterObject = (JObject)api.Get("/admin/customer_saved_searches.json");
            var filtersObject = filterObject.GetValue("customer_saved_searches");
            foreach (JObject filter in filtersObject)
            {
                summary.Add(filter);
            }

            return summary;
        }

        public JArray GetCustomerSavedSearch(string token, string shopName, int id, string[] fields)
        {
            var customers = new JArray();
            var authState = new ShopifyAuthorizationState();
            authState.AccessToken = token;
            authState.ShopName = shopName;
            var api = new ShopifyAPIClient(authState, new JsonDataTranslator());
            string fieldQuery = "";

            if (fields.Length != 0)
            {
                var commaSeparatedFields = string.Join(",", fields);
                fieldQuery = "?fields=" + commaSeparatedFields;
            }

            var filterObject = (JObject)api.Get(string.Format("/admin/customer_saved_searches/{0}/customers.json{1}", id, fieldQuery));
            var customersObject = filterObject.GetValue("customers");
            foreach (JObject customer in customersObject)
            {
                customers.Add(customer);
            }

            return customers;
        }
    }
}