using System.Net;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpForwarder();

var app = builder.Build();

var httpClient = new HttpMessageInvoker(new SocketsHttpHandler()
{
    UseProxy = false,
    AllowAutoRedirect = false,
    AutomaticDecompression = DecompressionMethods.None,
    UseCookies = false
});
var transformer = new CustomTransformer(); // or HttpTransformer.Default;
var requestConfig = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(100) };

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
}

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Map("/b/{*other}", async (HttpContext httpContext, IHttpForwarder forwarder) =>
{
    var error = await forwarder.SendAsync(httpContext, "http://smtp:80",
                httpClient, requestConfig, transformer);
    if (error != ForwarderError.None)
    {
        var errorFeature = httpContext.GetForwarderErrorFeature();
        var exception = errorFeature.Exception;
    }
});

app.Run();

class CustomTransformer : HttpTransformer
{
    public override async ValueTask TransformRequestAsync(HttpContext httpContext,
        HttpRequestMessage proxyRequest, string destinationPrefix)
    {
        // Copy headers normally and then remove the original host.
        // Use the destination host from proxyRequest.RequestUri instead.
        await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix);
        proxyRequest.Headers.Host = null;
    }
}
