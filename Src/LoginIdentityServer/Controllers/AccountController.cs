using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LoginIdentityServer.DbContext;
using LoginIdentityServer.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LoginIdentityServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController(UserManager<User> userManager, IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<string>> Register(RegisterRequest registerRequest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = new User
        {
            Email = registerRequest.Email,
            FullName = registerRequest.FullName,
            UserName = registerRequest.Email,
            PhoneNumber = registerRequest.PhoneNumber
        };

        var result = await userManager.CreateAsync(user, registerRequest.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        if (registerRequest.Roles is null)
        {
            await userManager.AddToRoleAsync(user, "User");
        }
        else
        {
            foreach (var role in registerRequest.Roles)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }


        return Ok(new AuthResponseDto
        {
            IsSuccess = true,
            Message = "Account Created Sucessfully!"
        });

    }

    //api/account/login
    [AllowAnonymous]
    [HttpPost("login")]

    public async Task<ActionResult<AuthResponseDto>> Login(LoginModel loginRequest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await userManager.FindByEmailAsync(loginRequest.Email);

        if (user is null)
        {
            return Unauthorized(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "User not found with this email",
            });
        }

        var result = await userManager.CheckPasswordAsync(user, loginRequest.Password);

        if (!result)
        {
            return Unauthorized(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Invalid Password."
            });
        }


        var token = GenerateToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            IsSuccess = true,
            Message = "Login Success."
        });


    }


    private string GenerateToken(User user)
    {
        var key = Encoding.ASCII
        .GetBytes(configuration.GetSection("JWTSetting").GetSection("securityKey").Value!);
        var roles = userManager.GetRolesAsync(user).Result;
        List<Claim> claims =
        [
            new (JwtRegisteredClaimNames.Email,user.Email??""),
        new (JwtRegisteredClaimNames.Name,user.FullName??""),
        new (JwtRegisteredClaimNames.NameId,user.Id ??""),
        new (JwtRegisteredClaimNames.Aud,configuration.GetSection("JWTSetting").GetSection("validAudience").Value!),
        new (JwtRegisteredClaimNames.Iss,configuration.GetSection("JWTSetting").GetSection("validIssuer").Value!),
        new(ClaimTypes.MobilePhone,user.PhoneNumber?? "")
        ];
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));
        JwtSecurityTokenHandler securityTokenHandler = new();
        SecurityToken token = securityTokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddSeconds(3600),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256
            )
        });
        return securityTokenHandler.WriteToken(token);


    }

    //api/account/detail
    [HttpGet("detail")]
    public async Task<ActionResult<UserDetailDto>> GetUserDetail()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await userManager.FindByIdAsync(currentUserId!);


        if (user is null)
        {
            return NotFound(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "User not found"
            });
        }

        return Ok(new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Roles = [.. await userManager.GetRolesAsync(user)],
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            AccessFailedCount = user.AccessFailedCount,

        });

    }


    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDetailDto>>> GetUsers()
    {
        var users = await userManager.Users.Select(u => new UserDetailDto
        {
            Id = u.Id,
            Email = u.Email,
            FullName = u.FullName,
            Roles = userManager.GetRolesAsync(u).Result.ToArray(),
            PhoneNumber = u.PhoneNumber,
        }).ToListAsync();

        return Ok(users);
    }
}
