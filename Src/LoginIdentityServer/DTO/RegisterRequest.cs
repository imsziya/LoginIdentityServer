﻿using System.ComponentModel.DataAnnotations;

namespace LoginIdentityServer.DTO;

public class RegisterRequest
{

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public List<string>? Roles { get; set; }
}
