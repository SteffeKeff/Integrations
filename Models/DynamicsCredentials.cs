using System.ComponentModel.DataAnnotations;

namespace Integrations.Models
{
    public class DynamicsCredentials
    {
        [Required]
        public string Domain { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }
    }
}