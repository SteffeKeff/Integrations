using System.ComponentModel.DataAnnotations;

namespace Integrations.Models
{
    public class SalesForceCredentials
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string SecurityToken { get; set; }
    }
}