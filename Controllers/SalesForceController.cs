using Integrations.Models;
using Integrations.Services;
using System;
using System.Web.Http;

namespace Integrations.Controllers
{

    [RoutePrefix("Salesforce")]
    public class SalesForceController : ApiController
    {

        SalesForceService service;

        [Route("Validate")]
        [HttpPost]
        public IHttpActionResult ValidateCredentials([FromUri]SalesForceCredentials credentials)
        {
            try
            {
                service = new SalesForceService();
                if (service.Validate(credentials))
                {
                    service.logout();

                    return Ok();
                }
            }
            catch (NullReferenceException)
            {
                return BadRequest();
            }

            return Unauthorized();
        }

        [Route("Campaigns")]
        [HttpPost]
        public IHttpActionResult GetAllCampaigns([FromUri]SalesForceCredentials credentials, [FromUri]bool translate = false)
        {
            try
            {
                service = new SalesForceService();

                if (service.Validate(credentials))
                {
                    var campaigns = service.GetCampaigns(translate);
                    service.logout();

                    return Ok(campaigns);
                }
            }
            catch (NullReferenceException)
            {
                return BadRequest();
            }

            return Unauthorized();
        }

        [Route("Campaigns/{campaignId}/Contacts")]
        [HttpPost]
        public IHttpActionResult GetAllContacts(string campaignId, [FromUri]SalesForceCredentials credentials, [FromUri] string[] fields, [FromUri]int top = 0, [FromUri]bool translate = true)
        {
            try
            {
                service = new SalesForceService();

                if (service.Validate(credentials))
                {
                    var contacts = service.GetContactsInCampaign(campaignId, translate, top, fields);
                    service.logout();

                    return Ok(contacts);
                }
            }
            catch (NullReferenceException)
            {
                return BadRequest();
            }

            return Unauthorized();
        }

    }
}
