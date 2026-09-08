using System.ComponentModel.DataAnnotations;

namespace Workbench.Modules.Attachments.Options;

/// <summary>
///     Options for configuring attachment validation and storage.
/// </summary>
public abstract class AttachmentOptions
{
    /// <summary>
    ///     Base key for all attachment options.
    /// </summary>
    protected const string BaseKey = "Attachments";

    /// <summary>
    ///     The maximum allowed file size for project leads in bytes.
    /// </summary>
    [Range(1, long.MaxValue)]
    public long MaxSizeBytesLead { get; set; }

    /// <summary>
    ///     The maximum allowed file size for project members in bytes.
    /// </summary>
    [Range(1, long.MaxValue)]
    public long MaxSizeBytesMember { get; set; }

    /// <summary>
    ///     The maximum number of attachments allowed per issue.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxCount { get; set; }

    /// <summary>
    ///     The allowed file extensions for uploaded attachments (e.g. .jpg, .png).
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<string> AllowedExtensions { get; set; } = [];
}
