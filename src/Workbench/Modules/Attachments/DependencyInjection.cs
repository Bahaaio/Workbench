using Workbench.Modules.Attachments.Services;
using Workbench.Modules.Attachments.Services.Implementations;

namespace Workbench.Modules.Attachments;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public void AddAttachmentsModule()
        {
            services.AddScoped<IAttachmentsReader, AttachmentsReader>();
            services.AddScoped<IAttachmentValidationService, AttachmentValidationService>();
        }
    }
}
