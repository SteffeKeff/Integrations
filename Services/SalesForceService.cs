using Newtonsoft.Json.Linq;
using Integrations.Models;
using Integrations.sForce;
using System;
using System.Collections;
using System.Text;
using System.Web.Services.Protocols;

namespace Integrations.Services
{
    public class SalesForceService
    {
        SforceService binding;

        public SalesForceService()
        {
            binding = new SforceService();
        }

        public bool Validate(SalesForceCredentials creds)
        {
            try
            {
                LoginResult lr;
                lr = binding.login(creds.UserName, creds.Password + creds.SecurityToken);
                var authEndPoint = binding.Url;
                binding.Url = lr.serverUrl;

                binding.SessionHeaderValue = new SessionHeader();
                binding.SessionHeaderValue.sessionId = lr.sessionId;
                return true;
            }
            catch (SoapException)
            {
                return false;
            }
        }

        public void logout()
        {
            binding.logout();
        }

        public JArray GetCampaigns(bool translate)
        {
            var entityType = "campaign";

            var fieldsQuery = getAllFieldsQuery(entityType);
            var query = string.Format("SELECT {0} FROM {1}", fieldsQuery, entityType);
            var queryResult = binding.query(query);
            var entities = prettiFyAndTranslate(queryResult, entityType, translate, new string[0]);

            return entities;
        }

        public JArray GetContactsInCampaign(string id, bool translate, int top, string[] fields)
        {
            var numberOfContacts = getNumberOfContacts(id, top);
            var campaignMemberQuery = getCampaignMembersQuery(id, top);
            var campaignMemberIds = getCampaignMemberIds(campaignMemberQuery);

            var contacts = getContactsFromCampaignMemberIds(campaignMemberIds, numberOfContacts, translate, fields);

            return contacts;
        }

        private JArray getContactsFromCampaignMemberIds(JArray campaignMemberIds, int numberOfContacts, bool translate, string[] fields)
        {
            var contacts = new JArray();
            var entityType = "contact";
            //
            string salesForceFields;

            if (fields.Length == 0)
            {
                salesForceFields = getAllFieldsQuery(entityType);
            }
            else
            {
                salesForceFields = string.Join(",", fields);
            }
            //
            var loops = (int)Math.Ceiling(((double)numberOfContacts / 500));

            for (int i = 0; i < loops; i++)
            {
                var someContactIds = getSomeContacts(campaignMemberIds, i * 500);
                var contactIdsQuery = buildContactIdsQuery(someContactIds);

                var queryContactsInCampaign = string.Format("SELECT {0} FROM Contact where Id IN ({1})", salesForceFields, contactIdsQuery);
                var queryResult = binding.query(queryContactsInCampaign);
                var campaignMembers = prettiFyAndTranslate(queryResult, entityType, translate, fields);

                foreach (var campaignMember in campaignMembers)
                {
                    contacts.Add(campaignMember);
                }
            }

            return contacts;
        }

        private JArray getCampaignMemberIds(string campaignMemberQuery)
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
                return string.Format("SELECT ContactId FROM CampaignMember where CampaignId = '{0}' AND ContactId != null", id);
            }
            else
            {
                return string.Format("SELECT ContactId FROM CampaignMember where CampaignId = '{0}' AND ContactId != null LIMIT {1}", id, top);
            }
        }

        private int getNumberOfContacts(string id, int top)
        {
            if (top == 0)
            {
                return binding.query(string.Format("SELECT count() FROM CampaignMember where CampaignId = '{0}' AND ContactId != null", id)).size;
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

        public JArray prettiFyAndTranslate(QueryResult queryResult, string entityType, bool translate, string[] customFields)
        {
            var entities = new JArray();
            var fields = getFields(entityType, customFields);

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

        private string getAllFieldsQuery(string entityType)
        {
            var fields = getFields(entityType, new string[0]);
            var fieldsQueryBuilder = new StringBuilder();

            foreach (var field in fields)
            {
                fieldsQueryBuilder.Append(field.name);
                fieldsQueryBuilder.Append(",");
            }

            fieldsQueryBuilder.Remove(fieldsQueryBuilder.Length - 1, 1); //Removes the last ","

            return fieldsQueryBuilder.ToString();
        }

        private Field[] getFields(string entityType, string[] fields)
        {
            var describeSObjectResults = binding.describeSObjects(new string[] { entityType });

            if (fields.Length != 0)
            {
                var salesForceFields = new Field[fields.Length];
                var allFieldsInArray = describeSObjectResults[0].fields;
                var allFields = new ArrayList();

                for (int i = 0; i < allFieldsInArray.Length; i++)
                {
                    allFields.Add(allFieldsInArray[i].name.ToLower());
                }

                int index = 0;

                for (int i = 0; i < fields.Length; i++)
                {
                    if (allFields.Contains(fields[i].ToLower()))
                    {
                        int fieldIndex = allFields.IndexOf(fields[i].ToLower());
                        salesForceFields[index] = allFieldsInArray[fieldIndex];
                        index++;
                    }
                }

                return salesForceFields;
            }
            else
            {
                return describeSObjectResults[0].fields;
            }
        }
    }
}