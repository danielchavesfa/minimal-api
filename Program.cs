using MinimalApi.Infraestrutura.Db;
using MinimalApi.DTOs;
using Microsoft.EntityFrameworkCore;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.Dominio.Servicos;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Dominio.ModelViews;
using MinimalApi.Dominio.Entidades;

#region Builder
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();
builder.Services.AddDbContext<DbContexto>(options =>
{
  options.UseMySql(
      builder.Configuration.GetConnectionString("mysql"),
      ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("mysql"))
    );
});

var app = builder.Build();
#endregion

#region Home
app.MapGet("/", () =>
{
  return Results.Json(new Home());
}).WithTags("Home");
#endregion

#region Administradores

app.MapPost("/adm/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
{
  if (administradorServico.Login(loginDTO) != null)
    return Results.Ok("Login com sucesso");
  else
    return Results.Unauthorized();
}).WithTags("Administradores");

#endregion

#region Veículos

static ErrorsDeValidacao validaDTO(VeiculoDTO veiculoDTO)
{
  var validacao = new ErrorsDeValidacao()
  {
    Mensagens = []
  };

  if (string.IsNullOrEmpty(veiculoDTO.Nome))
    validacao.Mensagens.Add("Digite um nome válido do carro.");

  if (string.IsNullOrEmpty(veiculoDTO.Marca))
    validacao.Mensagens.Add("Digite uma marca válida do carro.");

  if (veiculoDTO.Ano < 1950)
    validacao.Mensagens.Add("Só é permitido carros acima do ano 1950.");

  return validacao;
}

app.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
{
  var validacao = validaDTO(veiculoDTO);

  if (validacao.Mensagens.Count > 0)
    return Results.BadRequest(validacao);

  var novoVeiculo = new Veiculo
  {
    Nome = veiculoDTO.Nome,
    Marca = veiculoDTO.Marca,
    Ano = veiculoDTO.Ano
  };

  veiculoServico.Incluir(novoVeiculo);

  return Results.Created($"/veiculo/{novoVeiculo.Id}", novoVeiculo);
}
).WithTags("Veículos");

app.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculoServico veiculoServico) =>
{
  var veiculos = veiculoServico.Todos(pagina);

  return Results.Ok(veiculos);
}).WithTags("Veículos");

app.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
{
  var veiculo = veiculoServico.BuscarPorId(id);

  if (veiculo == null)
    return Results.NotFound();
  else
    return Results.Ok(veiculo);
}).WithTags("Veículos");

app.MapPut("/veiculos/{id}", ([FromRoute] int id, VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
{
  var veiculo = veiculoServico.BuscarPorId(id);

  if (veiculo == null)
    return Results.NotFound();

  var validacao = validaDTO(veiculoDTO);

  if (validacao.Mensagens.Count > 0)
    return Results.BadRequest(validacao);

  veiculo.Nome = veiculoDTO.Nome;
  veiculo.Marca = veiculoDTO.Marca;
  veiculo.Ano = veiculoDTO.Ano;
  veiculoServico.Atualizar(veiculo);

  return Results.Ok(veiculo);
}).WithTags("Veículos");

app.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
{
  var veiculo = veiculoServico.BuscarPorId(id);

  if (veiculo == null)
    return Results.NotFound();

  veiculoServico.Apagar(veiculo);

  return Results.NoContent();
}).WithTags("Veículos");

#endregion

#region App
app.UseSwagger();
app.UseSwaggerUI();

app.Run();
#endregion