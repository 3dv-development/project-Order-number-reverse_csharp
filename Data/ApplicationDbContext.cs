using Microsoft.EntityFrameworkCore;
using ProjectOrderNumberSystem.Models;

namespace ProjectOrderNumberSystem.Data
{
    /// <summary>
    /// データベースコンテキスト
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<EditHistory> EditHistories { get; set; }
        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // プロジェクト番号にユニーク制約
            modelBuilder.Entity<Project>()
                .HasIndex(p => p.ProjectNumber)
                .IsUnique();

            // 社員番号にユニーク制約
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.EmployeeId)
                .IsUnique();

            // 編集履歴との関連付け（カスケード削除）
            modelBuilder.Entity<Project>()
                .HasMany(p => p.EditHistory)
                .WithOne(e => e.Project)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // UpdatedAtの自動更新設定
            modelBuilder.Entity<Project>()
                .Property(p => p.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate();
        }
    }
}
