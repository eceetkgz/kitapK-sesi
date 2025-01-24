using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class AdminAuthMiddleware
{
    private readonly RequestDelegate _next;

    public AdminAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var isAdmin = context.Session.GetString("IsAdmin");
        
        if (context.Request.Path.StartsWithSegments("/Admin") && isAdmin != "true")
        {
            context.Response.Redirect("/Kullanici/GirisYap");
            return;
        }

        await _next(context);
    }
} 