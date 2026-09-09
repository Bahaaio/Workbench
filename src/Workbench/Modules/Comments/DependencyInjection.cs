using Microsoft.AspNetCore.Authorization;
using Workbench.Modules.Attachments.Services;
using Workbench.Modules.Comments.Authorization;
using Workbench.Modules.Comments.Models;
using Workbench.Modules.Comments.Options;
using Workbench.Modules.Comments.Services;
using Workbench.Modules.Comments.Services.Implementations;

namespace Workbench.Modules.Comments;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public void AddCommentsModule()
        {
            services.AddScoped<ICommentsService, CommentsService>();
            services.AddScoped<IAttachmentsService<Comment>, CommentAttachmentsService>();
            services.AddScoped<IAuthorizationHandler, CommentEditHandler>();

            services.AddOptions<CommentAttachmentOptions>()
                .BindConfiguration("Attachments:Comments")
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }
    }
}