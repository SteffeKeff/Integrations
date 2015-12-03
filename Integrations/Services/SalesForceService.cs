using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Web.Services.Protocols;

using Newtonsoft.Json.Linq;
using Integrations.Models;
using Integrations.sForce;

namespace Integrations.Services
{
    public class SalesForceService
    {
        readonly SforceService binding;

        public SalesForceService()
        {
            binding = new SforceService();
        }

        public bool Validate(SalesForceCredentials creds)
        {
            try
            {
                var loginResult = binding.login(creds.UserName, creds.Password + creds.SecurityToken);
                binding.Url = loginResult.serverUrl;

                binding.SessionHeaderValue = new SessionHeader {sessionId = loginResult.sessionId};
                return true;
            }
            catch (SoapException)
            {
                return false;
            }
        }

        public void Logout()
        {
            binding.logout();
        }

        public JArray GetCampaigns(bool translate)
        {
            var entityType = "campaign";

            var fieldsQuery = GetAllFieldsQuery(entityType);
            var query = $"SELECT {fieldsQuery} FROM {entityType}";
            var queryResult = binding.query(query);
            var entities = PrettiFyAndTranslate(queryResult, entityType, translate, new string[0]);

            return entities;
        }

        public JArray GetContactsInCampaign(string id, bool translate, int top, string[] fields)
        {
            var numberOfContacts = GetNumberOfContacts(id, top);
            var campaignMemberQuery = getCampaignMembersQuery(id, top);
            var campaignMemberIds = GetCampaignMemberIds(campaignMemberQuery);

            var contacts = GetContactsFromCampaignMemberIds(campaignMemberIds, numberOfContacts, translate, fields);

            return contacts;
        }

        private JArray GetContactsFromCampaignMemberIds(JArray campaignMemberIds, int numberOfContacts, bool translate, string[] fields)
        {
            var contacts = new JArray();
            var entityType = "contact";
            var salesForceFields = fields.Length == 0 ? GetAllFieldsQuery(entityType) : string.Join(",", fields);
            var loops = (int)Math.Ceiling(((double)numberOfContacts / 500));

            for (var i = 0; i < loops; i++)
            {
                var someContactIds = getSomeContacts(campaignMemberIds, i * 500);
                var contactIdsQuery = buildContactIdsQuery(someContactIds);

                var queryContactsInCampaign = $"SELECT {salesForceFields} FROM Contact where Id IN ({contactIdsQuery})";
                var queryResult = binding.query(queryContactsInCampaign);
                var campaignMembers = PrettiFyAndTranslate(queryResult, entityType, translate, fields);

                foreach (var campaignMember in campaignMembers)
                {
                    contacts.Add(campaignMember);
                }
            }

            return contacts;
        }

        private JArray GetCampaignMemberIds(string campaignMemberQuery)
        {
            var allCampaignMemberIds = new JArray();
            var campaignMemberIds = binding.query(campaignMemberQuery);

            foreach (var campaignMember in campaignMemberIds.records)
            {
                allCampaignMemberIds.Add(campaignMember.Any[0].InnerText);
            }

            while (!campaignMemberIds.done)
            {
                campaignMemberIds = binding.queryMore(campaignMemberIds.queryLocator);
                foreach (var campaignMember in campaignMemberIds.records)
                {
                    allCampaignMemberIds.Add(campaignMember.Any[0].InnerText);
                }
            }

            return allCampaignMemberIds;
        }

        private string getCampaignMembersQuery(string id, int top)
        {
            if (top == 0)
            {
                return $"SELECT ContactId FROM CampaignMember where CampaignId = '{id}' AND ContactId != null";
            }
            else
            {
                return $"SELECT ContactId FROM CampaignMember where CampaignId = '{id}' AND ContactId != null LIMIT {top}";
            }
        }

        private int GetNumberOfContacts(string id, int top)
        {
            if (top == 0)
            {
                return binding.query(
                    $"SELECT count() FROM CampaignMember where CampaignId = '{id}' AND ContactId != null").size;
            }
            else
            {
                return top;
            }
        }

        private JArray getSomeContacts(JArray campaignMemberIds, int loopCount)
        {
            JArray someContacts = new JArray();

            for (int i = 0; i < 500; i++)
            {
                if ((loopCount + i) == campaignMemberIds.Count) break;
                someContacts.Add(campaignMemberIds[loopCount + i]);
            }

            return someContacts;
        }

        private string buildContactIdsQuery(JArray contactIds)
        {
            var contactIdsString = new StringBuilder();

            foreach (var campaignMember in contactIds)
            {
                contactIdsString.Append("'" + campaignMember + "',");
            }
            contactIdsString.Remove(contactIdsString.Length - 1, 1); //Removes the last ","

            return contactIdsString.ToString();
        }

        public JArray PrettiFyAndTranslate(QueryResult queryResult, string entityType, bool translate, string[] customFields)
        {
            var entities = new JArray();
            var fields = GetFields(entityType, customFields);

            foreach (sObject attributes in queryResult.records)
            {
                JObject entity = new JObject();

                for (int i = 0; i < attributes.Any.Length; i++)
                {
                    var attribute = translate ? fields[i].label : fields[i].name;
                    entity.Add(attribute, attributes.Any[i].InnerText);
                }

                entities.Add(entity);
            }

            return entities;
        }

        private string GetAllFieldsQuery(string entityType)
        {
            var fields = GetFields(entityType, new string[0]);
            var fieldsQueryBuilder = new StringBuilder();

            foreach (var field in fields)
            {
                fieldsQueryBuilder.Append(field.name);
                fieldsQueryBuilder.Append(",");
            }

            fieldsQueryBuilder.Remove(fieldsQueryBuilder.Length - 1, 1); //Removes the last ","

            return fieldsQueryBuilder.ToString();
        }

        private Field[] GetFields(string entityType, IReadOnlyCollection<string> fields)
        {
            var describeSObjectResults = binding.describeSObjects(new[] { entityType });

            if (fields.Count != 0)
            {
                var salesForceFields = new Field[fields.Count];
                var allFieldsInArray = describeSObjectResults[0].fields;
                var allFields = new ArrayList();

                foreach (var field in allFieldsInArray)
                {
                    allFields.Add(field.name.ToLower());
                }

                var index = 0;

                foreach (var fieldIndex in from field in fields where allFields.Contains(field.ToLower()) select allFields.IndexOf(field.ToLower()))
                {
                    salesForceFields[index] = allFieldsInArray[fieldIndex];
                    index++;
                }

                return salesForceFields;
            }
            else
            {
                return describeSObjectResults[0].fields;
            }
        }

        public void Dispose()
        {
            binding.Dispose();
        }
    }
}