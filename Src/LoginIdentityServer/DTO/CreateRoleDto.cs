using System.ComponentModel.DataAnnotations;

namespace LoginIdentityServer.DTO;

public class CreateRoleDto
{
    [Required(ErrorMessage ="Role Name is required.")]
    public string RoleName { get; set; } = null!;
}