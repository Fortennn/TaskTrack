using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.Common.Interfaces;

namespace TaskTracker.Api.Controllers;

/// <summary>
/// Управління користувачами системи.
/// </summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// Отримує список зареєстрованих користувачів.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return Ok(users.Select(u => new UserSummaryDto(u.Id, u.Name, u.Email, u.Role.ToString())).ToList());
    }
}

public sealed record UserSummaryDto(Guid Id, string Name, string Email, string Role);
