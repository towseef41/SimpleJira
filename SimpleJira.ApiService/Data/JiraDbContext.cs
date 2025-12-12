using Microsoft.EntityFrameworkCore;
using SimpleJira.ApiService.Models;

namespace SimpleJira.ApiService.Data;

public class JiraDbContext : DbContext
{
    public JiraDbContext(DbContextOptions<JiraDbContext> options)
        : base(options) { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<IssueLink> IssueLinks => Set<IssueLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>()
            .HasIndex(p => p.Key)
            .IsUnique();

        modelBuilder.Entity<Project>(b =>
        {
            b.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            b.Property(p => p.Key)
                .IsRequired()
                .HasMaxLength(10);

            b.Property(p => p.Type)
                .IsRequired()
                .HasMaxLength(100);

            b.Property(p => p.Avatar)
                .IsRequired()
                .HasMaxLength(500);
        });

        modelBuilder.Entity<Issue>(b =>
        {
            b.Property(i => i.Title)
                .IsRequired()
                .HasMaxLength(200);

            b.Property(i => i.Summary)
                .HasMaxLength(4000);

            b.Property(i => i.StoryPoints)
                .HasDefaultValue(null);

            b.HasOne(i => i.Assignee)
                .WithMany()
                .HasForeignKey("AssigneeId")
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(i => i.Reporter)
                .WithMany()
                .HasForeignKey("ReporterId")
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Comment>(b =>
        {
            b.Property(c => c.Body)
                .IsRequired()
                .HasMaxLength(4000);

            b.HasOne(c => c.Issue)
                .WithMany(i => i.Comments)
                .HasForeignKey(c => c.IssueId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IssueLink>(b =>
        {
            b.HasIndex(l => new { l.IssueId, l.LinkedIssueId })
                .IsUnique();

            b.HasOne(l => l.Issue)
                .WithMany(i => i.Links)
                .HasForeignKey(l => l.IssueId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne<Issue>()
                .WithMany()
                .HasForeignKey(l => l.LinkedIssueId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Simple seed data for dropdowns
        var softwareId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var businessId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
        var serviceDeskId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3");

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = softwareId, Name = "Software" },
            new Category { Id = businessId, Name = "Business" },
            new Category { Id = serviceDeskId, Name = "Service Desk" }
        );

        var janeId = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
        var johnId = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");

        modelBuilder.Entity<User>().HasData(
            new User { Id = janeId, Name = "Jane Product" },
            new User { Id = johnId, Name = "John Developer" }
        );
    }
}
