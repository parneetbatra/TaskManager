using Microsoft.EntityFrameworkCore;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Infrastructure.DbContext
{
    public sealed class TaskManagerDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public TaskManagerDbContext(DbContextOptions<TaskManagerDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaskItem> Tasks => Set<TaskItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TaskItem>(builder =>
            {
                builder.ToTable("Tasks");
                builder.HasKey(t => t.Id);

                builder.Property(t => t.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                builder.Property(t => t.Description)
                    .HasMaxLength(1000);

                builder.Property(t => t.Status)
                    .HasConversion<int>()
                    .IsRequired();

                builder.Property(t => t.CreatedAt)
                    .IsRequired();

                builder.Property(t => t.UpdatedAt)
                    .IsRequired();
            });
        }
    }
}
