using MudBlazor;

namespace Workbench.Components.Shared;

public static class AppDialogOptions
{
    public static readonly DialogOptions Small = new() { MaxWidth = MaxWidth.Small, FullWidth = true };
    public static readonly DialogOptions Medium = new() { MaxWidth = MaxWidth.Medium, FullWidth = true };
}
