using System.ComponentModel.DataAnnotations;

namespace Integrations.Models
{
    public class DynamicsCredentials : ICredentials
    {
        public string UserName { get; set; }

        public string Password { get; set; }

        [Required]
        public string Domain { get; set; }

        [Required]
        public string Region { get; set; }
    }
}