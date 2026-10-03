using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Common;
using TaskTracker.Application.Common.Interfaces;
using TaskTracker.Application.DTOs.Projects;
using TaskTracker.Application.DTOs.Tasks;
using TaskTracker.Domain.Entities;
using TaskTracker.Infrastructure.Persistence;

namespace TaskTracker.Infrastructure.Persistence.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly ApplicationDbContext _context;

    public ProjectRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<ProjectDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProjectDetailDto(
                p.Id,
                p.Name,
                p.Description,
                p.CreatedAt,
                p.Tasks
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new TaskDto(
                        t.Id,
                        t.Title,
                        t.Description,
                        t.Status,
                        t.Priority,
                        t.DueDate,
                        t.ProjectId,
                        p.Name,
                        t.AssigneeId,
                        t.Assignee != null ? t.Assignee.Name : null,
                        t.Comments.Count,
                        t.CreatedAt))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<ProjectDto>> GetPagedAsync(ProjectQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        IQueryable<Project> query = _context.Projects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim();
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));
        }

        query = (parameters.SortBy.ToLowerInvariant(), parameters.Order.ToLowerInvariant()) switch
        {
            ("name", "asc") => query.OrderBy(p => p.Name).ThenBy(p => p.Id),
            ("name", "desc") => query.OrderByDescending(p => p.Name).ThenBy(p => p.Id),
            ("createdat", "asc") => query.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id),
            _ => query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(p => new ProjectDto(
                p.Id,
                p.Name,
                p.Description,
                p.Tasks.Count,
                p.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProjectDto>(
            items,
            totalCount,
            parameters.Page,
            parameters.PageSize);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Projects.AnyAsync(p => p.Id == id, cancellationToken);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _context.Projects.AddAsync(project, cancellationToken);
    }

    public void Update(Project project)
    {
        _context.Projects.Update(project);
    }

    public void Delete(Project project)
    {
        _context.Projects.Remove(project);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
