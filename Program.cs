var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapPost("/login", (MinimalApi.DTOs.LoginDTO loginDTO) =>
{
  if (loginDTO.Email == "admin@test.com" && loginDTO.Senha == "112233")
    return Results.Ok("Login com sucesso");
  else
    return Results.Unauthorized();
});

app.Run();

