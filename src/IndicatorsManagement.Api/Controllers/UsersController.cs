using System.Security.Claims;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Constants;
using IndicatorsManagement.Contracts.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndicatorsManagement.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin},{Roles.EntityAdmin}")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    private int CallerEntityId => int.TryParse(User.FindFirstValue("EntityId"), out var eid) ? eid : 0;
    private string CallerRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] int? entityId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        // Entity_Admin can only see their own entity's users
        if (CallerRole == Roles.EntityAdmin)
            entityId = CallerEntityId;

        var result = await _userService.GetUsersAsync(entityId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var result = await _userService.GetUserByIdAsync(id, CallerEntityId, CallerRole);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // Entity_Admin can only create users for their own entity
        if (CallerRole == Roles.EntityAdmin && request.EntityId != CallerEntityId)
            return Forbid();

        var creatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _userService.CreateUserAsync(request, creatorId);
        if (!result.Success)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetUser), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var updaterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _userService.UpdateUserAsync(id, request, updaterId, CallerEntityId, CallerRole);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        var deactivatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _userService.DeactivateUserAsync(id, deactivatorId);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
}
