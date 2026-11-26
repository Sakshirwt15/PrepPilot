using Microsoft.EntityFrameworkCore;
using PrepPilot.Application.Abstractions;
using PrepPilot.Domain.Entities;

namespace PrepPilot.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<MockTest> MockTests => Set<MockTest>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Option> Options => Set<Option>();
    public DbSet<Attempt> Attempts => Set<Attempt>();
    public DbSet<AttemptAnswer> AttemptAnswers => Set<AttemptAnswer>();
    public DbSet<Problem> Problems => Set<Problem>();
    public DbSet<ProblemSubmission> ProblemSubmissions => Set<ProblemSubmission>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>(e =>
        {
            e.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.Property(u => u.PasswordHash).IsRequired().HasMaxLength(100);
            e.HasIndex(u => u.Email).IsUnique();
        });

        mb.Entity<Company>(e =>
        {
            e.Property(c => c.Name).IsRequired().HasMaxLength(100);
            e.Property(c => c.Slug).IsRequired().HasMaxLength(60);
            e.Property(c => c.Emoji).HasMaxLength(16);
            e.Property(c => c.Description).HasMaxLength(1000);
            e.Property(c => c.Difficulty).IsRequired().HasMaxLength(20);
            e.HasIndex(c => c.Slug).IsUnique();

            e.HasMany(c => c.MockTests).WithOne(t => t.Company)
                .HasForeignKey(t => t.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<MockTest>(e =>
        {
            e.Property(t => t.Title).IsRequired().HasMaxLength(200);
            e.Property(t => t.Description).HasMaxLength(1000);
            e.Property(t => t.Difficulty).IsRequired().HasMaxLength(20);
            e.HasIndex(t => t.CompanyId);

            e.HasMany(t => t.Questions).WithOne(q => q.MockTest)
                .HasForeignKey(q => q.MockTestId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(t => t.Attempts).WithOne(a => a.MockTest)
                .HasForeignKey(a => a.MockTestId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Question>(e =>
        {
            e.Property(q => q.Text).IsRequired().HasMaxLength(1000);
            e.Property(q => q.Topic).IsRequired().HasMaxLength(100);
            e.Property(q => q.Explanation).HasMaxLength(1000);
            e.HasMany(q => q.Options).WithOne(o => o.Question)
                .HasForeignKey(o => o.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Option>(e => e.Property(o => o.Text).IsRequired().HasMaxLength(300));

        mb.Entity<Attempt>(e =>
        {
            e.HasIndex(a => new { a.UserId, a.MockTestId });
            e.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(a => a.Answers).WithOne(x => x.Attempt)
                .HasForeignKey(x => x.AttemptId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Problem>(e =>
        {
            e.Property(p => p.Title).IsRequired().HasMaxLength(200);
            e.Property(p => p.Slug).IsRequired().HasMaxLength(120);
            e.Property(p => p.Difficulty).IsRequired().HasMaxLength(20);
            e.Property(p => p.Topics).HasMaxLength(300);
            e.Property(p => p.Companies).HasMaxLength(300);
            e.Property(p => p.FunctionName).IsRequired().HasMaxLength(80);
            e.HasIndex(p => p.Slug).IsUnique();
        });

        mb.Entity<ProblemSubmission>(e =>
        {
            e.Property(s => s.Language).IsRequired().HasMaxLength(20);
            e.HasIndex(s => new { s.UserId, s.ProblemId });
            e.HasOne(s => s.Problem).WithMany().HasForeignKey(s => s.ProblemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
