using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Description;

using Integrations.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Discovery;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Integrations.Services
{
    public class DynamicsService : IDisposable
    {
        private readonly OrganizationServiceProxy organizationServiceProxy;
        private readonly OrganizationDetailCollection organizations;

        public DynamicsService(DynamicsCredentials credentials) : this(credentials, "") { }

        public DynamicsService(DynamicsCredentials credentials, string orgName)
        {
            var organizationUniqueName = orgName;
            var region = credentials.Region;
            var discoveryServiceAddress = $"https://disco.{region}.dynamics.com/XRMServices/2011/Discovery.svc";

            var discoveryProxy = GetDiscoveryServiceProxy(discoveryServiceAddress, credentials);
            organizations = DiscoverOrganizations(discoveryProxy);

            var organization =
                        organizations.FirstOrDefault(detail => detail.UniqueName.Equals(organizationUniqueName)) ??
                        organizations.FirstOrDefault();


            organizationServiceProxy = GetOrganizationServiceProxy(organization, discoveryServiceAddress, credentials);
        }

        // TODO: Must be set some other way
        public OrganizationDetailCollection GetOrganizations()
        {
            return organizations;
        }

        public EntityCollection GetAllLists()
        {
            var query = new QueryExpression { EntityName = "list", ColumnSet = new ColumnSet(true) };
            query.AddOrder("modifiedon", OrderType.Descending);

            var allLists = organizationServiceProxy.RetrieveMultiple(query);

            return allLists;
        }

        public List<Contact> GetContactsInList(string id, string[] fields, int top)
        {
            var listMembersQuery = createListMemberQuery(id, top);
            var contactsQuery = getContactsQuery(fields);
            var allContacts = GetAllContacts(contactsQuery, listMembersQuery);

            return allContacts;
        }


        private List<Contact> GetAllContacts(QueryExpression contactsQuery, QueryExpression listMembersQuery)
        {
            var allContacts = new List<Contact>();
            EntityCollection listMembers;

            do
            {
                listMembers = organizationServiceProxy.RetrieveMultiple(listMembersQuery);
                listMembersQuery.PageInfo.PageNumber++;
                listMembersQuery.PageInfo.PagingCookie = listMembers.PagingCookie;

                contactsQuery.Criteria = new FilterExpression();
                var condition = new ConditionExpression
                {
                    AttributeName = "contactid",
                    Operator = ConditionOperator.In
                };

                foreach (var listMember in listMembers.Entities.Cast<ListMember>())
                {
                    condition.Values.Add(listMember.EntityId.Id);
                }

                contactsQuery.Criteria.AddCondition(condition);
                var contacts = organizationServiceProxy.RetrieveMultiple(contactsQuery);

                allContacts.AddRange(contacts.Entities.Cast<Contact>());

            } while (listMembers.MoreRecords);

            return allContacts;
        }

        private QueryExpression createListMemberQuery(string id, int top)
        {
            var listMembersQuery = new QueryExpression { EntityName = "listmember", ColumnSet = new ColumnSet("listid", "entityid") };
            var listid = new Guid(id);

            if (top == 0 || top > 5000)
            {
                var listMembersPaging = new PagingInfo
                {
                    Count = 5000,
                    PageNumber = 1,
                    PagingCookie = null
                };

                listMembersQuery.PageInfo = listMembersPaging;
            }
            else
            {
                listMembersQuery.TopCount = top;
            }

            listMembersQuery.Criteria = new FilterExpression();
            listMembersQuery.Criteria.AddCondition("listid", ConditionOperator.Equal, listid);

            return listMembersQuery;
        }

        private QueryExpression getContactsQuery(string[] fields)
        {
            if (fields.Length == 0) //Returnerar en query med bestämda attributes eller samtliga
            {
                return new QueryExpression { EntityName = "contact", ColumnSet = new ColumnSet(true) };
            }
            else
            {
                var columnSet = new ColumnSet();
                fields = fields.Select(s => s.ToLowerInvariant()).ToArray();
                columnSet.AddColumns(fields);

                return new QueryExpression { EntityName = "contact", ColumnSet = columnSet };
            }
        }

        public void ChangeBulkEmail(string contactToUpdate)
        {
            var contact = organizationServiceProxy.Retrieve("contact", new Guid(contactToUpdate), new ColumnSet("donotbulkemail")).ToEntity<Contact>();

            contact.DoNotBulkEMail = contact.DoNotBulkEMail != true;

            organizationServiceProxy.Update(contact);
        }

        public IEnumerable<AttributeDisplayName> GetAttributeDisplayName(string entitySchemaName)
        {
            var service = organizationServiceProxy;
            var req = new RetrieveEntityRequest
            {
                RetrieveAsIfPublished = true,
                LogicalName = entitySchemaName,
                EntityFilters = EntityFilters.Attributes
            };

            var resp = (RetrieveEntityResponse)service.Execute(req);

            return resp.EntityMetadata.Attributes.Select(a => new AttributeDisplayName
            {
                DisplayName = a.DisplayName.LocalizedLabels.Count > 0 ? a.DisplayName.LocalizedLabels[0].Label : a.LogicalName,
                LogicalName = a.LogicalName
            });
        }

        public class AttributeDisplayName
        {
            public string LogicalName { get; set; }
            public string DisplayName { get; set; }
        }

        public DiscoveryServiceProxy GetDiscoveryServiceProxy(string discoveryServiceAddress, DynamicsCredentials credentials)
        {
            var serviceManagement = ServiceConfigurationFactory.CreateManagement<IDiscoveryService>(new Uri(discoveryServiceAddress));

            var endpointType = serviceManagement.AuthenticationType;
            var authCredentials = GetCredentials(serviceManagement, endpointType, credentials);
            var discoveryProxy = GetProxy<IDiscoveryService, DiscoveryServiceProxy>(serviceManagement, authCredentials);
            return discoveryProxy;
        }

        public OrganizationServiceProxy GetOrganizationServiceProxy(OrganizationDetail organization, string discoveryServiceAddress, DynamicsCredentials dynamicsCredentials)
        {
            if (organization == null)
                throw new ArgumentNullException(nameof(organization));

            var serviceManagement = ServiceConfigurationFactory.CreateManagement<IDiscoveryService>(new Uri(discoveryServiceAddress));

            var endpointType = serviceManagement.AuthenticationType;

            var orgServiceManagement =
                ServiceConfigurationFactory.CreateManagement<IOrganizationService>(
                new Uri(organization.Endpoints[EndpointType.OrganizationService]));

            var credentials = GetCredentials(orgServiceManagement, endpointType, dynamicsCredentials);

            var proxy = GetProxy<IOrganizationService, OrganizationServiceProxy>(orgServiceManagement, credentials);
            proxy.EnableProxyTypes();

            return proxy;
        }

        private AuthenticationCredentials GetCredentials<TService>(IServiceManagement<TService> service, AuthenticationProviderType endpointType, DynamicsCredentials credentials)
        {
            var authCredentials = new AuthenticationCredentials();
            var userName = credentials.UserName;
            var password = credentials.Password;
            var domain = credentials.Domain;

            switch (endpointType)
            {
                case AuthenticationProviderType.ActiveDirectory:
                    authCredentials.ClientCredentials.Windows.ClientCredential =
                        new System.Net.NetworkCredential(userName,
                            password,
                            domain);
                    break;
                case AuthenticationProviderType.LiveId:
                    authCredentials.ClientCredentials.UserName.UserName = userName;
                    authCredentials.ClientCredentials.UserName.Password = password;
                    authCredentials.SupportingCredentials = new AuthenticationCredentials
                    {
                        ClientCredentials = Microsoft.Crm.Services.Utility.DeviceIdManager.LoadOrRegisterDevice()
                    };
                    break;
                default:
                    authCredentials.ClientCredentials.UserName.UserName = userName;
                    authCredentials.ClientCredentials.UserName.Password = password;

                    if (endpointType == AuthenticationProviderType.OnlineFederation)
                    {
                        var provider = service.GetIdentityProvider(authCredentials.ClientCredentials.UserName.UserName);
                        if (provider != null && provider.IdentityProviderType == IdentityProviderType.LiveId)
                        {
                            authCredentials.SupportingCredentials = new AuthenticationCredentials
                            {
                                ClientCredentials =
                                    Microsoft.Crm.Services.Utility.DeviceIdManager.LoadOrRegisterDevice()
                            };
                        }
                    }

                    break;
            }

            return authCredentials;
        }

        public OrganizationDetailCollection DiscoverOrganizations(IDiscoveryService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var orgRequest = new RetrieveOrganizationsRequest();
            var orgResponse = (RetrieveOrganizationsResponse)service.Execute(orgRequest);

            return orgResponse.Details;
        }

        private TProxy GetProxy<TService, TProxy>(
            IServiceManagement<TService> serviceManagement,
            AuthenticationCredentials authCredentials)
            where TService : class
            where TProxy : ServiceProxy<TService>
        {
            var classType = typeof(TProxy);

            if (serviceManagement.AuthenticationType == AuthenticationProviderType.ActiveDirectory)
            {
                var constructorInfo = classType
                    .GetConstructor(new[] { typeof(IServiceManagement<TService>), typeof(ClientCredentials) });
                return (TProxy) constructorInfo?.Invoke(new object[] { serviceManagement, authCredentials.ClientCredentials });
            }

            var tokenCredentials = serviceManagement.Authenticate(authCredentials);
            var constructor = classType.GetConstructor(new[] { typeof(IServiceManagement<TService>), typeof(SecurityTokenResponse) });
            return (TProxy) constructor?.Invoke(new object[] { serviceManagement, tokenCredentials.SecurityTokenResponse });
        }

        public void Dispose()
        {
            organizationServiceProxy.Dispose();
        }
    }
}
