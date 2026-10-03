using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Common;
using TaskTracker.Application.Common.Interfaces;
using TaskTracker.Application.DTOs.Comments;
using TaskTracker.Application.DTOs.Tasks;
using TaskTracker.Domain.Entities;
using TaskTracker.Infrastructure.Persistence;

namespace TaskTracker.Infrastructure.Persistence.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly ApplicationDbContext _context;

    public TaskRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<TaskDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TaskDetailDto(
                t.Id,
                t.Title,
                t.Description,
                t.Status,
                t.Priority,
                t.DueDate,
                t.ProjectId,
                t.Project.Name,
                t.AssigneeId,
                t.Assignee != null ? t.Assignee.Name : null,
                t.CreatedAt,
                t.Comments
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new CommentDto(
                        c.Id,
                        c.TaskId,
                        c.AuthorId,
                        c.Author.Name,
                        c.Text,
                        c.CreatedAt))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<TaskDto>> GetPagedAsync(TaskQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        IQueryable<TaskItem> query = _context.Tasks.AsNoTracking();

        // 1. Фильтрация
        if (parameters.ProjectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == parameters.ProjectId.Value);
        }

        if (parameters.AssigneeId.HasValue)
        {
            query = query.Where(t => t.AssigneeId == parameters.AssigneeId.Value);
        }

        if (parameters.Status.HasValue)
        {
            query = query.Where(t => t.Status == parameters.Status.Value);
        }

        if (parameters.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == parameters.Priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim();
            query = query.Where(t => EF.Functions.ILike(t.Title, $"%{search}%"));
        }

        if (parameters.DueDateFrom.HasValue)
        {
            var fromUtc = parameters.DueDateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(t => t.DueDate >= fromUtc);
        }

        if (parameters.DueDateTo.HasValue)
        {
            var exclusiveToUtc = parameters.DueDateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(t => t.DueDate < exclusiveToUtc);
        }

        // 2. Детерминированная сортировка
        query = (parameters.SortBy.ToLowerInvariant(), parameters.Order.ToLowerInvariant()) switch
        {
            ("title", "asc") => query.OrderBy(t => t.Title).ThenBy(t => t.Id),
            ("title", "desc") => query.OrderByDescending(t => t.Title).ThenBy(t => t.Id),
            ("duedate", "asc") => query.OrderBy(t => t.DueDate).ThenBy(t => t.Id),
            ("duedate", "desc") => query.OrderByDescending(t => t.DueDate).ThenBy(t => t.Id),
            ("priority", "asc") => query.OrderBy(t => t.Priority).ThenBy(t => t.Id),
            ("priority", "desc") => query.OrderByDescending(t => t.Priority).ThenBy(t => t.Id),
            ("status", "asc") => query.OrderBy(t => t.Status).ThenBy(t => t.Id),
            ("status", "desc") => query.OrderByDescending(t => t.Status).ThenBy(t => t.Id),
            ("createdat", "asc") => query.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id),
            _ => query.OrderByDescending(t => t.CreatedAt).ThenBy(t => t.Id)
        };

        // 3. Подсчет общего количества до пагинации
        var totalCount = await query.CountAsync(cancellationToken);

        // 4. Пагинация и проекция в DTO без N+1
        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(t => new TaskDto(
                t.Id,
                t.Title,
                t.Description,
                t.Status,
                t.Priority,
                t.DueDate,
                t.ProjectId,
                t.Project.Name,
                t.AssigneeId,
                t.Assignee != null ? t.Assignee.Name : null,
                t.Comments.Count,
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<TaskDto>(
            items,
            totalCount,
            parameters.Page,
            parameters.PageSize);
    }

    public async Task<IReadOnlyList<CommentDto>> GetCommentsByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await _context.Comments
            .AsNoTracking()
            .Where(c => c.TaskId == taskId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentDto(
                c.Id,
                c.TaskId,
                c.AuthorId,
                c.Author.Name,
                c.Text,
                c.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<Comment> AddCommentAsync(Guid taskId, Guid authorId, string text, CancellationToken cancellationToken = default)
    {
        var taskExists = await _context.Tasks.AnyAsync(t => t.Id == taskId, cancellationToken);
        if (!taskExists)
        {
            throw new InvalidOperationException($"Task with Id '{taskId}' does not exist.");
        }

        var authorExists = await _context.Users.AnyAsync(u => u.Id == authorId, cancellationToken);
        if (!authorExists)
        {
            throw new InvalidOperationException($"User with Id '{authorId}' does not exist.");
        }

        var comment = new Comment(taskId, authorId, text.Trim());
        await _context.Comments.AddAsync(comment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return comment;
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await _context.Tasks.AddAsync(task, cancellationToken);
    }

    public void Update(TaskItem task)
    {
        _context.Tasks.Update(task);
    }

    public void Delete(TaskItem task)
    {
        _context.Tasks.Remove(task);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
