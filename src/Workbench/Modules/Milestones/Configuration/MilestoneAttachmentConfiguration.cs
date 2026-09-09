using Workbench.Modules.Milestones.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Workbench.Modules.Milestones.Configuration;

public class MilestoneAttachmentConfiguration : IEntityTypeConfiguration<MilestoneAttachment>
{
    public void Configure(EntityTypeBuilder<MilestoneAttachment> builder)
    {
        builder.Property(a => a.ParentId)
            .HasColumnName("MilestoneId");

        builder.HasOne(a => a.Milestone)
            .WithMany(m => m.Attachments)
            .HasForeignKey(a => a.ParentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
