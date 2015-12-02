using System.Web.Http;

using Integrations.Models;
using Integrations.Services;

namespace Integrations.Controllers
{

    [RoutePrefix("Salesforce")]
    public class SalesForceController : ApiController
    {

        private SalesForceService crmService;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            crmService?.Dispose();
        }

        [Route("Validate")]
        [HttpPost]
        public IHttpActionResult ValidateCredentials([FromUri]SalesForceCredentials credentials)
        {
            if(!ModelState.IsValid)
            {
                return Unauthorized();
            }
            
            crmService = new SalesForceService();

            if (crmService.Validate(credentials))
            {
                return Ok();
            }
            else
            {
                return Unauthorized();
            }
           
        }

        [Route("Campaigns")]
        [HttpPost]
        public IHttpActionResult GetAllCampaigns([FromUri]SalesForceCredentials credentials, [FromUri]bool translate = false)
        {
            if (!ModelState.IsValid)
            {
                return Unauthorized();
            }
            
            crmService = new SalesForceService();

            if (crmService.Validate(credentials))
            {
                var campaigns = crmService.GetCampaigns(translate);

                return Ok(campaigns);
            }
            else
            {
                return Unauthorized();
            }
           
        }

        [Route("Campaigns/{campaignId}/Contacts")]
        [HttpPost]
        public IHttpActionResult GetAllContacts(string campaignId, [FromUri]SalesForceCredentials credentials, [FromUri] string[] fields, [FromUri]int top = 0, [FromUri]bool translate = true)
        {
            if (!ModelState.IsValid)
            {
                return Unauthorized();
            }
            
            crmService = new SalesForceService();

            if (crmService.Validate(credentials))
            {
                var contacts = crmService.GetContactsInCampaign(campaignId, translate, top, fields);

                return Ok(contacts);
            }
            else
            {
                return Unauthorized();
            }
            
        }

    }
}
