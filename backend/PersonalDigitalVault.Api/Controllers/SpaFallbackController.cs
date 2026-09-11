using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace PersonalDigitalVault.Api.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[AllowAnonymous]
public sealed class SpaFallbackController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public SpaFallbackController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet("{*path}", Order = int.MaxValue)]
    public IActionResult Get(string? path)
    {
        string requestPath =
            "/" +
            (path ?? string.Empty)
                .TrimStart('/');

        // API routes must never be handled by the Angular SPA fallback.
        if (
            requestPath.Equals(
                "/api",
                StringComparison.OrdinalIgnoreCase
            )
            ||
            requestPath.StartsWith(
                "/api/",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return NotFound();
        }

        // Missing static assets must remain 404 instead of receiving index.html.
        //
        // Legacy /html/*.html aliases are intentionally allowed because the
        // Angular router already owns those compatibility redirects.
        bool isLegacyHtmlAlias =
            requestPath.StartsWith(
                "/html/",
                StringComparison.OrdinalIgnoreCase
            )
            &&
            requestPath.EndsWith(
                ".html",
                StringComparison.OrdinalIgnoreCase
            );

        if (
            Path.HasExtension(requestPath)
            &&
            !isLegacyHtmlAlias
        )
        {
            return NotFound();
        }

        string webRoot =
            _environment.WebRootPath
            ??
            Path.Combine(
                _environment.ContentRootPath,
                "wwwroot"
            );

        string indexPath =
            Path.Combine(
                webRoot,
                "index.html"
            );

        // Local API-only development can run without a built Angular bundle.
        if (!System.IO.File.Exists(indexPath))
        {
            return NotFound();
        }

        Response.Headers["Cache-Control"] =
            "no-cache, no-store, must-revalidate";

        return PhysicalFile(
            indexPath,
            "text/html; charset=utf-8"
        );
    }
}
