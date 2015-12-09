using System.Linq;
using System.Web.Http;
using System.Collections.Generic;
using System.ServiceModel.Security;
using System.Web.Http.Cors;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json.Linq;
using Integrations.Models;
using Integrations.Services;

namespace Integrations.Controllers
{

    [EnableCors(origins: "http://localhost:8888", headers: "*", methods: "*")]
    [RoutePrefix("Dynamics")]
    public class DynamicsController : ApiController
    {
        private DynamicsService crmService;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            crmService?.Dispose();
        }

        [Route("Proxies")]
        [HttpGet]
        public IHttpActionResult GetProxies()
        {
            var discoveryUrls = new[] { "crm4", "crm", "crm9", "crm5", "crm6", "crm7", "crm2"};
            var regions = new[]
            {
                "Europe, Middle East and Africa", "North America", "North America 2", "Asia Pacific Area",
                "Oceania","Japan", "South America"
            };

            var proxies = discoveryUrls.Select((t, count) => new DynamicsProxy
            {
                DiscoveryUrl = $"https://disco.{t}.dynamics.com/XRMServices/2011/Discovery.svc", Region = regions[count]
            }).ToList();

            return Ok(proxies);
        }

        [Route("Validate")]
        [HttpPost]
        public IHttpActionResult ValidateCredentials([FromUri]DynamicsCredentials credentials)
        {
            if (!ModelState.IsValid)
            {
                return Unauthorized();
            }

            try
            {
                if (!string.IsNullOrEmpty(credentials.Domain))
                {
                    crmService = new DynamicsService(credentials);
                }
                else
                {
                    //Will create service and try to fetch all lists to validate
                    new DynamicsService(credentials).GetAllMarketLists(); 
                }

                return Ok();
            }
            catch (MessageSecurityException)
            {
                return Unauthorized();
            }
            catch(SecurityAccessDeniedException)
            {
                return Unauthorized();
            }
            catch (SecurityNegotiationException)
            {
                return Unauthorized();
            }
        }

        [Route("Organizations")]
        [HttpPost]
        public IHttpActionResult GetOrganizations([FromUri]DynamicsCredentials credentials)
        {
            if (!ModelState.IsValid)
            {
                return Unauthorized();
            }

            try
            {
                crmService = new DynamicsService(credentials);
                var organizations = crmService.GetOrganizations().Select(o => new
                {
                    o.UniqueName,
                    o.FriendlyName
                });

                return Ok(organizations);
            }
            catch (MessageSecurityException)
            {
                return Unauthorized();
            }
            catch (SecurityAccessDeniedException)
            {
                return Unauthorized();
            }
        }

        [Route("Fields")]
        [HttpPost]
        public IHttpActionResult GetContactFields([FromUri]DynamicsCredentials credentials)
        {
            if (!ModelState.IsValid)
            {
                return Unauthorized();
            }

            try
            {
                crmService = new DynamicsService(credentials);

                var attributes = crmService.GetAttributeDisplayName("contact");

                return Ok(attributes);
            }
            catch (MessageSecurityException)
            {
                return Unauthorized();
            }
            catch (SecurityAccessDeniedException)
            {
                return Unauthorized();
            }
        }

        [Route("MarketLists")]
        [HttpPost]
        public IHttpActionResult GetMarketLists([FromUri]DynamicsCredentials credentials, [FromUri] bool translate = false)
        {
            if (!ModelState.IsValid)
            {
                return Unauthorized();
            }

            try
            {
                crmService = new DynamicsService(credentials);

                var lists = crmService.GetAllMarketLists();
                var goodLookingLists = GetValuesFromLists(lists, translate);

                return Ok(goodLookingLists);
            }
            catch (MessageSecurityException)
            {
                return Unauthorized();
            }
            catch (SecurityAccessDeniedException)
            {
                return Unauthorized();
            }
        }

        [Route("MarketLists/{listId}/Contacts")]
        [HttpPost]
        public IHttpActionResult GetContacts(string listId, [FromUri]DynamicsCredentials credentials, [FromUri] string[] fields, [FromUri] int top = 0, [FromUri] bool translate = true)
        {
            try
            {
                crmService = new DynamicsService(credentials);

                var contacts = crmService.GetContactsInList(listId, fields, top);
                var goodLookingContacts = GetValuesFromContacts(contacts, translate, fields);

                return Ok(goodLookingContacts);
            }
            catch (MessageSecurityException)
            {
                return Unauthorized();
            }
            catch (SecurityAccessDeniedException)
            {
                return Unauthorized();
            }
        }

        [Route("Contacts/{contactId}/Donotbulkemail")]
        [HttpPut]
        public IHttpActionResult UpdateBulkEmailForContact(string contactId, [FromUri]DynamicsCredentials credentials)
        {
            try
            {
                crmService = new DynamicsService(credentials);

                crmService.ChangeBulkEmail(contactId);

                return Ok();
            }
            catch (MessageSecurityException)
            {
                return Unauthorized();
            }
            catch (SecurityAccessDeniedException)
            {
                return Unauthorized();
            }
        }

        private JArray GetValuesFromLists(EntityCollection lists, bool translate)
        {
            var jsonLists = new JArray();

            foreach (var list in lists.Entities)
            {
                var jsonList = new JObject();

                foreach (var prop in list.GetType().GetProperties())
                {
                    if (prop.Name == "Item") continue;

                    var val = prop.GetValue(list, null);
                        
                    jsonList[prop.Name] = val?.ToString();
                }

                jsonLists.Add(jsonList);
            }

            if (translate)
            {
                jsonLists = TranslatePropertiesToDisplayName(jsonLists, "list");
            }

            return jsonLists;
        }

        public JArray GetValuesFromContacts(List<Contact> contacts, bool translate, string[] fields)
        {
            var jsonContacts = new JArray();

            foreach (var contact in contacts)
            {
                var jsonContact = new JObject();

                if (fields.Length == 0)
                {
                    foreach (var prop in contact.GetType().GetProperties().Where(prop => prop.Name != "Item"))
                    {
                        if (prop.Name == "Attributes")
                        {
                            var attributes = (AttributeCollection)prop.GetValue(contact, null);

                            foreach (var attribute in attributes)
                            {
                                jsonContact[attribute.Key] = attribute.Value?.ToString();
                            }
                        }

                        var val = prop.GetValue(contact, null);
                        if(val == null)    
                            jsonContact[prop.Name] = null;//val?.ToString();
                    }
                }
                else
                {
                    foreach (var field in fields)
                    {
                        jsonContact[field] = contact.GetAttributeValue<object>(field)?.ToString().ToLower();
                    }
                }

                jsonContacts.Add(jsonContact);
            }

            if (translate)
            {
                jsonContacts = TranslatePropertiesToDisplayName(jsonContacts, "contact");
            }

            return jsonContacts;
        }

        public JArray TranslatePropertiesToDisplayName(JArray array, string type)
        {
            var displayNames = crmService.GetAttributeDisplayName(type).ToList();
            var contactsWithDisplayNames = new JArray();

            foreach (var contact in array.Children<JObject>())
            {
                var newContact = new JObject();
                foreach (var property in contact.Properties())
                {
                    var displayName = displayNames.SingleOrDefault(d => d.LogicalName == property.Name.ToLower());
                    if(displayName == null)
                        continue;

                    if (newContact.Property(displayName.DisplayName) == null)
                    {
                        newContact.Add(displayName.DisplayName, property.Value);
                    }
                    else
                    {
                        var suffix = 2;
                        while (newContact.Property(displayName.DisplayName + ' ' + suffix) != null)
                            suffix++;

                        newContact.Add(displayName.DisplayName + ' ' + suffix, property.Value);
                    }
                }
                contactsWithDisplayNames.Add(newContact);
            }

            return contactsWithDisplayNames;
        }

        public class DynamicsProxy
        {
            public string Region { get; set; }
            public string DiscoveryUrl { get; set; }
        }

    }
}