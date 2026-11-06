using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.DTOs.Jobs;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Exceptions;

namespace TaskTracker.Api.Controllers;

/// <summary>
/// Управління та моніторинг статусу фонових задач.
/// </summary>
[ApiController]
[Route("jobs")]
[Route("api/jobs")]
[Produces("application/json")]
public sealed class JobsController(
    IJobStatusStore jobStatusStore)
    : ControllerBase
{
    /// <summary>
    /// Отримує поточний стан виконання фонової задачі за її jobId.
    /// </summary>
    /// <param name="id">Унікальний глобальний ідентифікатор фонової задачі (UUID).</param>
    /// <response code="200">Стан фонової задачі (pending, processing, done, failed, cancelled).</response>
    /// <response code="404">Фонову задачу з указаним ідентифікатором не знайдено (Problem Details).</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<JobStatusDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<JobStatusDto> GetById(Guid id)
    {
        var status = jobStatusStore.GetStatus(id);
        if (status is null)
        {
            throw new NotFoundException(
                $"Background job with id '{id}' does not exist.");
        }

        return Ok(new JobStatusDto(
            id,
            status.Value.ToString().ToLowerInvariant()));
    }
}
