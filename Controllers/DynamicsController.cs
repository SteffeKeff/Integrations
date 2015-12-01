using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.ServiceModel.Security;

using Integrations.Models;
using Integrations.Services;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json.Linq;

namespace Integrations.Controllers
{

    [RoutePrefix("Dynamics")]
    public class DynamicsController : ApiController
    {
        private DynamicsService crmService;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (crmService != null)
                crmService.Dispose();
        }

        /// <summary>
        /// This method will try to get organizations matching passed credentials.If it fails it returns Unauthorized.
        /// </summary>
        // TODO: Should be split up into Validate + GetOrganizations
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
                crmService = new DynamicsService(credentials);

                return Ok();
            }
            catch (MessageSecurityException)
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
        }


        [Route("Organizations/{orgName}/MarketLists")]
        [HttpPost]
        public IHttpActionResult GetMarketListsWithAllAttributes(string orgName, [FromUri]DynamicsCredentials credentials, [FromUri] bool translate = false, [FromUri] bool allAttributes = true)
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
        }

        [Route("Organizations/{orgName}/MarketLists/{listId}/Contacts")]
        [HttpPost]
        public IHttpActionResult GetContactsWithAttributes(string orgName, string listId, [FromUri]DynamicsCredentials credentials, [FromUri] string[] fields, [FromUri] int top = 0, [FromUri] bool translate = true)
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
        }

        [Route("Organizations/{orgName}/Contacts/{contactId}/Donotbulkemail")]
        [HttpPut]
        public IHttpActionResult UpdateBulkEmailForContact(string orgName, string contactId, [FromUri]DynamicsCredentials credentials)
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
                        
                        jsonList[prop.Name] = val == null ? null : val.ToString();
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
                            
                            jsonContact[prop.Name] = val == null? null : val.ToString();
                        }
                    }
                }
                else
                {
                    foreach (var field in fields)
                    {
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
    }
}