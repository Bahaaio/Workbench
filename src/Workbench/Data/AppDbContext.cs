using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Workbench.Modules.Attachments.Models;
using Workbench.Modules.Auth.Models;
using Workbench.Modules.Comments.Models;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Issues.Votes.Models;
using Workbench.Modules.Kanban.Models;
using Workbench.Modules.Milestones.Models;
using Workbench.Modules.Projects.Invites.Models;
using Workbench.Modules.Projects.Memberships.Models;
using Workbench.Modules.Projects.Models;
using Workbench.Modules.Tags.Models;

namespace Workbench.Data;

public class AppDbContext(DbContextOptions options)
    : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>(options)
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectMembership> ProjectMemberships { get; set; }
    public DbSet<Board> Boards { get; set; }
    public DbSet<BoardColumn> BoardColumns { get; set; }
    public DbSet<BoardCard> BoardCards { get; set; }
    public DbSet<Milestone> Milestones { get; set; }
    public DbSet<MilestoneItem> MilestoneItems { get; set; }
    public DbSet<Issue> Issues { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Vote> Votes { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<ProjectInvite> ProjectInvites { get; set; }
    public DbSet<IssueStatusChange> IssueStatusChanges { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}