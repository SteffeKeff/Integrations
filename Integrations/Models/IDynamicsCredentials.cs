using System.ComponentModel.DataAnnotations;

namespace Integrations.Models
{
    public interface IDynamicsCredentials
    {
        [Required]
        string UserName { get; set; }

        [Required]
        string Password { get; set; }
    }
}