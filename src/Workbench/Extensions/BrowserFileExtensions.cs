using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;

namespace Workbench.Extensions;

public static class BrowserFileExtensions
{
    public static async Task<IFormFile> ToFormFile(this IBrowserFile file, long maxBytes)
    {
        await using var source = file.OpenReadStream(maxBytes);
        using var stream = new MemoryStream();
        await source.CopyToAsync(stream);
        stream.Position = 0;

        return new FormFile(stream, 0, stream.Length, "file", file.Name)
        {
            Headers = new HeaderDictionary(),
            ContentType = file.ContentType ?? "application/octet-stream"
        };
    }
}
