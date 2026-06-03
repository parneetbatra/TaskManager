using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Dtos.Responses;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.DbContext;
using TaskStatus = TaskManager.Domain.Enums.TaskStatus;

namespace TaskManager.Infrastructure.Repositories
{
    public sealed class EfTaskRepository : ITaskRepository
    {
        private readonly TaskManagerDbContext _dbContext;

        public EfTaskRepository(TaskManagerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<TaskItem>> GetAllAsync()
            => await _dbContext.Tasks.AsNoTracking().ToListAsync();

        public async Task<PagedResult<TaskItem>> GetPagedAsync(TaskStatus? statusFilter, int page, int pageSize)
        {
            var query = _dbContext.Tasks.AsNoTracking().AsQueryable();
            if (statusFilter.HasValue)
            {
                query = query.Where(task => task.Status == statusFilter.Value);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(task => task.UpdatedAt)
                .ThenByDescending(task => task.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalPages = totalCount == 0
                ? 1
                : (int)Math.Ceiling(totalCount / (double)pageSize);

            return new PagedResult<TaskItem>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                Items = items
            };
        }

        public async Task<TaskItem?> GetByIdAsync(Guid id)
            => await _dbContext.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

        public async Task<TaskItem> CreateAsync(TaskItem task)
        {
            _dbContext.Tasks.Add(task);
            await _dbContext.SaveChangesAsync();
            return task;
        }

        public async Task<bool> UpdateAsync(TaskItem task)
        {
            _dbContext.Tasks.Update(task);
            var saved = await _dbContext.SaveChangesAsync();
            return saved > 0;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var item = await _dbContext.Tasks.FindAsync(id);
            if (item is null)
            {
                return false;
            }

            _dbContext.Tasks.Remove(item);
            var saved = await _dbContext.SaveChangesAsync();
            return saved > 0;
        }
    }
}
