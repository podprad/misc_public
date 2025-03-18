using TXTextControl.Web;
using TXTextControl.Web.MVC.DocumentViewer;

namespace TestServer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(
                builder =>
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
        }); 

        builder.Services.AddControllers();

        var app = builder.Build();

        app.MapGet("/", () => "TX Text Control .NET Server for ASP.NET Backend is up and running.");
        app.UseRouting();
        app.UseCors();
        app.UseWebSockets();
        app.UseTXWebSocketMiddleware();
        app.UseTXDocumentViewer();

        app.Run();
    }
}