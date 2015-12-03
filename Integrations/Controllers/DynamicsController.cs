using System;
using System.Linq;
using System.Web.Http;
using System.Collections.Generic;
using System.ServiceModel.Security;

using Microsoft.Xrm.Sdk;
using Newtonsoft.Json.Linq;
using Integrations.Models;
using Integrations.Services;

namespace Integrations.Controllers
{

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
            var hosts = new[] {"crm", "crm9", "crm4", "crm5", "crm6", "crm7", "crm2"};
            var regions = new[]
            {
                "North America", "North America 2", "Europe, Middle East and Africa", "Asia Pacific Area",
                "Oceania","Japan", "South America"
            };

            var proxies = hosts.Select((t, count) => new DynamicsProxy
            {
                //Host = $"https://dev.{t}.dynamics.com/XRMServices/2011/Discovery.svc", Region = regions[count]
                Host = hosts[count], Region = regions[count]
            }).ToList();

            return Ok(proxies);
        }

        [Route("Validate")]
        [HttpPost]
        public IHttpActionResult ValidateCredentials([FromUri]HostedCredentials credentials)
        {
            if (!ModelState.IsValid)
            {
                return Unauthorized();
            }

            try
            {
                crmService = new DynamicsService(credentials);

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
        }

        [Route("ValidateOnPremise")]
        [HttpPost]
        public IHttpActionResult ValidateCredentials([FromUri]OnPremiseCredentials credentials)
        {
            if (!ModelState.IsValid)
            {
                return Unauthorized();
            }

            try
            {
                new DynamicsService(credentials).GetAllLists(); //Will create seervice and try to fetch all lists to validate

                return Ok();
            }
            catch (SecurityNegotiationException)
            {
                return Unauthorized();
            }
            catch (SecurityAccessDeniedException)
            {
                return Unauthorized();
            }
            catch (InvalidOperationException)
            {
                return BadRequest();
            }
        }

        [Route("Organizations")]
        [HttpPost]
        public IHttpActionResult GetOrganizations([FromUri]HostedCredentials credentials)
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

        [Route("MarketLists")]
        [HttpPost]
        public IHttpActionResult GetMarketLists([FromUri]OnPremiseCredentials credentials, [FromUri] bool translate = false)
        {
            if (!ModelState.IsValid)
            {
                return Unauthorized();
            }

            try
            {
                crmService = new DynamicsService(credentials);

                var lists = crmService.GetAllLists();
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
            catch (SecurityNegotiationException)
            {
                return Unauthorized();
            }
        }


        [Route("Organizations/{orgName}/MarketLists")]
        [HttpPost]
        public IHttpActionResult GetMarketLists(string orgName, [FromUri]HostedCredentials credentials, [FromUri] bool translate = false)
        {
            if (!ModelState.IsValid)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(orgName))
                return BadRequest("No organisation name passed");

            try
            {
                crmService = new DynamicsService(credentials, orgName);

                var lists = crmService.GetAllLists();
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

        [Route("Organizations/{orgName}/MarketLists/{listId}/Contacts")]
        [HttpPost]
        public IHttpActionResult GetContacts(string orgName, string listId, [FromUri]HostedCredentials credentials, [FromUri] string[] fields, [FromUri] int top = 0, [FromUri] bool translate = true)
        {
            try
            {
                crmService = new DynamicsService(credentials, orgName);

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

        [Route("MarketLists/{listId}/Contacts")]
        [HttpPost]
        public IHttpActionResult GetContacts(string listId, [FromUri]OnPremiseCredentials credentials, [FromUri] string[] fields, [FromUri] int top = 0, [FromUri] bool translate = true)
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

        [Route("Organizations/{orgName}/Contacts/{contactId}/Donotbulkemail")]
        [HttpPut]
        public IHttpActionResult UpdateBulkEmailForContact(string orgName, string contactId, [FromUri]HostedCredentials credentials)
        {
            try
            {
                crmService = new DynamicsService(credentials, orgName);

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

        [Route("Contacts/{contactId}/Donotbulkemail")]
        [HttpPut]
        public IHttpActionResult UpdateBulkEmailForContact(string contactId, [FromUri]OnPremiseCredentials credentials)
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
                    if (prop.Name != "Item")
                    {
                        var val = prop.GetValue(list, null);
                        
                        jsonList[prop.Name] = val?.ToString();
                    }
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

            foreach (Contact contact in contacts)
            {
                var jsonContact = new JObject();

                if (fields.Length == 0)
                {
                    foreach (var prop in contact.GetType().GetProperties())
                    {
                        if (prop.Name != "Item")
                        {
                            var val = prop.GetValue(contact, null);
                            
                            jsonContact[prop.Name] = val?.ToString();
                        }
                    }
                }
                else
                {
                    foreach (var field in fields)
                    {
                        if(contact.GetAttributeValue<object>(field) != null)
                            jsonContact[field] = contact.GetAttributeValue<object>(field).ToString().ToLower();
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
            public string Host { get; set; }
    }

    }
}