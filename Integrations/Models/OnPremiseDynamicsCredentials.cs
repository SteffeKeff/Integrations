
using System.ComponentModel.DataAnnotations;

namespace Integrations.Models
{
    public class OnPremiseCredentials : IDynamicsCredentials
    {   
        public string UserName { get; set; }

        public string Password { get; set; }

        [Required]
        public string Host { get; set; }
    }
}