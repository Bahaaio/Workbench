using Workbench.Common.Exceptions;
using Workbench.Modules.Attachments;
using Workbench.Modules.Attachments.Options;

namespace Workbench.Modules.Attachments.Services;

/// <summary>
///     Validates uploaded attachments according to configuration and business rules.
/// </summary>
public interface IAttachmentValidationService
{
    /// <summary>
    ///     Validates an uploaded attachment file (size, extension, etc).
    /// </summary>
    /// <param name="file">The file to validate.</param>
    /// <param name="options">The attachment options to use for validation.</param>
    /// <param name="maxSizeBytes">The maximum file size in bytes.</param>
    /// <exception cref="BadRequestException">Thrown on validation failure</exception>
    void Validate(IFormFile file, AttachmentOptions options, long maxSizeBytes);

    /// <summary>
    ///     Validates the number of attachments allowed per context (e.g., issue).
    /// </summary>
    /// <param name="count">The current number of attachments.</param>
    /// <param name="maxCount">The maximum number of attachments allowed.</param>
    /// <exception cref="BadRequestException">Thrown on validation failure</exception>
    void ValidateCount(int count, int maxCount);
}
