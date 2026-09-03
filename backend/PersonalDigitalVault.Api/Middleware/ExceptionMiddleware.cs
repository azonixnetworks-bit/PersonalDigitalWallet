using System.Net; using System.Text.Json;
namespace PersonalDigitalVault.Api.Middleware;
public class ExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {try
        {
            await next(context);
        }

        catch(UnauthorizedAccessException ex)
        {
            await Write(context,HttpStatusCode.Unauthorized,ex.Message);
        }
        catch(KeyNotFoundException ex)
        {
            await Write(context,HttpStatusCode.NotFound,ex.Message);
        }
        catch(ArgumentException ex)
        {
            await Write(context,HttpStatusCode.BadRequest,ex.Message);
        }
        catch(Exception)
        {
            await Write(context,HttpStatusCode.InternalServerError,"An unexpected server error occurred.");
        }
    }
    private static async Task Write(HttpContext c,HttpStatusCode status,string message)
    {
        c.Response.StatusCode=(int)status;c.Response.ContentType="application/json";
        await c.Response.WriteAsync(JsonSerializer.Serialize(new{message}));
    }
}
