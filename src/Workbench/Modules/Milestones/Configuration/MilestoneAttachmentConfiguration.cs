using Workbench.Modules.Milestones.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Workbench.Modules.Milestones.Configuration;

public class MilestoneAttachmentConfiguration : IEntityTypeConfiguration<MilestoneAttachment>
{
    public void Configure(EntityTypeBuilder<MilestoneAttachment> builder)
    {
        builder.Property(ta => ta.ParentId)
            .HasColumnName("MilestoneId");

        builder.HasOne(ta => ta.Milestone)
            .WithMany(m => m.Attachments)
            .HasForeignKey(ta => ta.ParentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
