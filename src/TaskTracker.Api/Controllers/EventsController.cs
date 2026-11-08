using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.Common;
using TaskTracker.Application.DTOs.Events;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Exceptions;

namespace TaskTracker.Api.Controllers;

/// <summary>
/// Управління подіями, пошуком з пагінацією та реєстраціями учасників.
/// </summary>
[ApiController]
[Route("events")]
[Route("api/events")]
[Produces("application/json")]
public sealed class EventsController(IEventService eventService) : ControllerBase
{
    /// <summary>
    /// Отримує список подій з фільтрацією за датою, категорією, пошуковим рядком, сортуванням та пагінацією.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<EventDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<EventDto>>> GetPaged(
        [FromQuery] EventQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await eventService.GetPagedAsync(parameters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Отримує інформацію про подію за її ідентифікатором.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType<EventDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailDto>> GetById(string id, CancellationToken cancellationToken)
    {
        var item = await eventService.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            throw new NotFoundException($"Event with ID '{id}' was not found.");
        }
        return Ok(item);
    }

    /// <summary>
    /// Створює нову подію та визначає координати за допомогою Geoapify API.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<EventDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailDto>> Create(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var eventItem = await eventService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = eventItem.Id },
            eventItem);
    }

    /// <summary>
    /// Оновлює дані події.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType<EventDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailDto>> Update(
        string id,
        [FromBody] UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await eventService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Реєструє учасника на подію.
    /// </summary>
    [HttpPost("{id}/registrations")]
    [ProducesResponseType<RegistrationDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegistrationDto>> RegisterAttendee(
        string id,
        [FromBody] CreateRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var registration = await eventService.RegisterAttendeeAsync(id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, registration);
    }
}
