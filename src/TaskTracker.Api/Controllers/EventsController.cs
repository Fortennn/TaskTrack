using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.DTOs.Events;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Exceptions;

namespace TaskTracker.Api.Controllers;

/// <summary>
/// Управління подіями з автоматичним зовнішнім геокодуванням адрес через Geoapify API.
/// </summary>
[ApiController]
[Route("events")]
[Route("api/events")]
[Produces("application/json")]
public sealed class EventsController(IEventService eventService) : ControllerBase
{
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
    /// Створює нову подію та визначає координати (Latitude, Longitude) за допомогою Geoapify API.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<EventDetailDto>(StatusCodes.Status201Created)]
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
    /// Оновлює дані події та оновлює координати лише за зміни адреси.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType<EventDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailDto>> Update(
        string id,
        [FromBody] UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await eventService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }
}
