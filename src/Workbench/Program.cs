using ApexCharts;
using Workbench.Common.Exceptions;
using Workbench.Components;
using Workbench.Extensions;

var builder = WebApplication.CreateBuilder(args);

// API services
builder.Services.AddControllers();
builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddModules();
builder.Services.AddOpenApiServices();
builder.Services.AddExceptionHandling();

// UI services
builder.Services.AddUiServices();
builder.Services.AddApexCharts();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseOpenApiUi();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseExceptionHandler();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/projects")).AllowAnonymous();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode().AllowAnonymous();

app.Run();