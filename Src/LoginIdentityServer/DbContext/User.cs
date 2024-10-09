using Microsoft.AspNetCore.Identity;

namespace LoginIdentityServer.DbContext;

public class User : IdentityUser
{
    public string? FullName { get; set; }
}