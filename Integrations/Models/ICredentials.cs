using System.ComponentModel.DataAnnotations;

namespace Integrations.Models
{
    public interface ICredentials
    {
        [Required]
        string UserName { get; set; }

        [Required]
        string Password { get; set; }
    }
}