using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Integrations.Services
{
    public class JetshopService
    {
        public dynamic GetProducts(string url, int top)
        {
            string jsonString;
            using (var wc = new WebClient())
            {

                jsonString = wc.DownloadString(url);
            }
            dynamic cleanJson = JsonConvert.DeserializeObject(jsonString);
            var prodItems = cleanJson.ProductItems;
            var jObj = new JObject[top];
            if (top == 0) return prodItems;

            for (var i = 0; i < top; i++)
            {
                jObj.SetValue(prodItems[i], i);
            }
            return jObj;
        }
    }
}