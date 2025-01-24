using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class YoneticiDogrulamaMiddleware
{
    private readonly RequestDelegate _sonraki;

    public YoneticiDogrulamaMiddleware(RequestDelegate sonraki)
    {
        _sonraki = sonraki;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var yoneticiMi = context.Session.GetString("IsAdmin");
        
        if (context.Request.Path.StartsWithSegments("/Yonetici") && yoneticiMi != "true")
        {
            context.Response.Redirect("/Kullanici/GirisYap");
            return;
        }

        await _sonraki(context);
    }
} 