using Workbench.Common.Exceptions;
using Workbench.Modules.Attachments;
using Workbench.Modules.Attachments.Options;

namespace Workbench.Modules.Attachments.Services.Implementations;

public class AttachmentValidationService : IAttachmentValidationService
{
    private readonly ILogger<AttachmentValidationService> _logger;

    public AttachmentValidationService(ILogger<AttachmentValidationService> logger)
    {
        _logger = logger;
    }

    public void Validate(IFormFile file, AttachmentOptions options, long maxSizeBytes)
    {
        ValidateSize(file, maxSizeBytes);
        ValidateExtension(file, options);
    }

    public void ValidateCount(int count, int maxCount)
    {
        if (count > maxCount)
            throw new BadRequestException($"Maximum number of attachments exceeded ({maxCount})");
    }

    private void ValidateExtension(IFormFile file, AttachmentOptions options)
    {
        var extension = Path.GetExtension(file.FileName);

        if (!options.AllowedExtensions.Contains(extension))
        {
            _logger.LogWarning("File extension is not allowed: {Extension}", extension);
            throw new BadRequestException("File extension is not allowed");
        }
    }

    private void ValidateSize(IFormFile file, long maxSizeBytes)
    {
        if (file.Length == 0)
        {
            _logger.LogWarning("File is empty");
            throw new BadRequestException("File cannot be empty");
        }

        if (file.Length > maxSizeBytes)
        {
            _logger.LogWarning("File size exceeds maximum allowed: {Size}", file.Length);
            throw new BadRequestException($"File cannot exceed {maxSizeBytes} bytes");
        }
    }
}
