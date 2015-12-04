using System.ComponentModel.DataAnnotations;

namespace Integrations.Models
{
    public class DynamicsCredentials
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string DiscoveryUrl { get; set; }

        public string Domain { get; set; }

        public string Organization { get; set; }
    }
}