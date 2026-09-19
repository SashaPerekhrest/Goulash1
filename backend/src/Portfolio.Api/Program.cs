var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { service = "Portfolio.Api", status = "running" }));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
