// Ignore Spelling: Dto

using LoginIdentityServer.DbContext;
using LoginIdentityServer.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginIdentityServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RolesController(RoleManager<IdentityRole> roleManager, UserManager<User> userManager) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto createRoleDto)
    {
        if (string.IsNullOrEmpty(createRoleDto.RoleName))
        {
            return BadRequest("Role name is required");
        }

        var roleExist = await roleManager.RoleExistsAsync(createRoleDto.RoleName);

        if (roleExist)
        {
            return BadRequest("Role already exist");
        }

        var roleResult = await roleManager.CreateAsync(new IdentityRole(createRoleDto.RoleName) { ConcurrencyStamp = DateTime.UtcNow.ToString() });

        if (roleResult.Succeeded)
        {
            return Ok(new { message = "Role Created successfully" });
        }

        return BadRequest("Role creation failed.");

    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleResponseDto>>> GetRoles()
    {


        // list of roles with total users in each role 

        var roles = await roleManager.Roles.Select(r => new RoleResponseDto
        {
            Id = r.Id,
            Name = r.Name,
            TotalUsers = userManager.GetUsersInRoleAsync(r.Name!).Result.Count
        }).ToListAsync();

        return Ok(roles);
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(string id)
    {
        // find role by their id

        var role = await roleManager.FindByIdAsync(id);

        if (role is null)
        {
            return NotFound("Role not found.");
        }

        var result = await roleManager.DeleteAsync(role);

        if (result.Succeeded)
        {
            return Ok(new { message = "Role deleted successfully." });
        }

        return BadRequest("Role deletion failed.");

    }


    [HttpPost("assign")]
    public async Task<IActionResult> AssignRole([FromBody] RoleAssignDto roleAssignDto)
    {
        var user = await userManager.FindByIdAsync(roleAssignDto.UserId);

        if (user is null)
        {
            return NotFound("User not found.");
        }

        var role = await roleManager.FindByIdAsync(roleAssignDto.RoleId);

        if (role is null)

        {
            return NotFound("Role not found.");
        }

        var result = await userManager.AddToRoleAsync(user, role.Name!);

        if (result.Succeeded)
        {
            return Ok(new { message = "Role assigned successfully" });
        }

        var error = result.Errors.FirstOrDefault();

        return BadRequest(error!.Description);

    }
}
