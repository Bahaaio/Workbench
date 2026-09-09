using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Models;

namespace Workbench.Modules.Projects.Configuration;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.Visibility)
            .IsRequired()
            .HasDefaultValue(ProjectVisibility.Public);

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(p => p.Owner)
            .WithMany(o => o.OwnedProjects)
            .HasForeignKey(p => p.OwnerId);
    }
}