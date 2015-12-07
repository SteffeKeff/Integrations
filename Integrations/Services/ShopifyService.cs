using System;
using System.Linq;
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
            var returnUrl = new Uri(CALLBACK_URL);
            authorizer = new ShopifyAPIAuthorizer(shopName, API_KEY, API_SECRET);
            var authUrl = authorizer.GetAuthorizationURL(rights, returnUrl.ToString());
            return authUrl;
        }

        public dynamic GetAccessToken(string code, string shopName)
        {
            authorizer = new ShopifyAPIAuthorizer(shopName, API_KEY, API_SECRET);
            var authState = authorizer.AuthorizeClient(code);

            return authState?.AccessToken;
        }

        public dynamic GetEntities(string entity, string token, string shopName, string[] fields, int top, string collectionId)
        {
            var authState = new ShopifyAuthorizationState
            {
                AccessToken = token,
                ShopName = shopName
            };
            var api = new ShopifyAPIClient(authState, new JsonDataTranslator());

            var fieldQuery = "";
            var summary = new JArray();

            if (fields.Length != 0)
            {
                var commaSeparatedFields = string.Join(",", fields);
                fieldQuery = "fields=" + commaSeparatedFields + "&";
            }

            if (collectionId != null)
            {
                fieldQuery = fieldQuery + "collection_id=" + collectionId + "&";
            }

            if (top == 0 || top > 250)
            {
                dynamic count = api.Get($"/admin/{entity}/count.json");

                JObject json = count;
                double realCount = int.Parse(json.GetValue("count").ToString());
                realCount = realCount / 250;
                var loops = (int)Math.Ceiling(realCount);

                for (var i = 1; i <= loops; i++)
                {
                    CollectEntities(api, summary, entity, 250, i, fieldQuery);
                }
            }
            else
            {
                CollectEntities(api, summary, entity, top, 1, fieldQuery);
            }
            return summary;
        }

        public dynamic GetEntity(string entity, string id, string token, string shopName, string[] fields, int top)
        {
            var authState = new ShopifyAuthorizationState
            {
                AccessToken = token,
                ShopName = shopName
            };

            var api = new ShopifyAPIClient(authState, new JsonDataTranslator());

            var queryString = "";
            dynamic entityObject;
            if (fields.Length != 0)
            {
                var commaSeparatedFields = string.Join(",", fields);
                queryString = "fields=" + commaSeparatedFields + "&";
            }

            if (entity.Equals("collections"))
            {
                entity = "products";
                entityObject = GetEntities(entity, token, shopName, fields, top, id);
                return entityObject;
            }if (entity.Equals("customer_saved_searches"))
            {
                id = id + "/customers";
                if (top != 0)
                {
                    queryString = queryString + "limit=" + top;
                }
            }
            string urlSuffix = $"/admin/{entity}/{id}.json?{queryString}";
            entityObject = (JObject)api.Get(urlSuffix);

            return entityObject;
        }

        private static void CollectEntities(ShopifyAPIClient api, JArray summary, string entity, int top, int page, string fieldQuery)
        {
            var entityObject = (JObject)api.Get($"/admin/{entity}.json?{fieldQuery}limit={top}&page={page}");
            var entitiesObject = entityObject.GetValue($"{entity}");
            foreach (var entityObj in entitiesObject.Cast<JObject>())
            {
                summary.Add(entityObj);
            }
        }
    }
}