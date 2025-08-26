using MinimalApi.Infraestrutura.Db;
using MinimalApi.DTOs;
using Microsoft.EntityFrameworkCore;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.Dominio.Servicos;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Dominio.ModelViews;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Enums;

#region Builder
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();
builder.Services.AddDbContext<DbContexto>(options =>
{
  options.UseMySql(
      builder.Configuration.GetConnectionString("MySql"),
      ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("MySql"))
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

app.MapPost("/adm/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico admServico) =>
{
  if (admServico.Login(loginDTO) != null)
    return Results.Ok("Login com sucesso");
  else
    return Results.Unauthorized();
}).WithTags("Administradores");

app.MapPost("/adm", ([FromBody] AdministradorDTO admDTO, IAdministradorServico admServico) =>
{
  var validacao = new ErrorsDeValidacao { Mensagens = [] };

  if (string.IsNullOrEmpty(admDTO.Email))
    validacao.Mensagens.Add("Você precisa digitar em e-mail válido.");
  if (string.IsNullOrEmpty(admDTO.Senha))
    validacao.Mensagens.Add("Digite uma senha válida.");
  if (admDTO.Perfil == null)
    validacao.Mensagens.Add("Perfil não pode ser vazio.");

  if (validacao.Mensagens.Count > 0)
    return Results.BadRequest(validacao);

  var novoAdm = new Administrador
  {
    Email = admDTO.Email,
    Senha = admDTO.Senha,
    Perfil = admDTO.Perfil.ToString() ?? Perfil.Editor.ToString()
  };

  admServico.Incluir(novoAdm);

  return Results.Created($"/adm", new AdministradorModelView
  {
    Id = novoAdm.Id,
    Email = novoAdm.Email,
    Perfil = novoAdm.Perfil
  });
}).WithTags("Administradores");

app.MapGet("/adms", ([FromQuery] int? pagina, IAdministradorServico admServico) =>
{
  var listAdms = new List<AdministradorModelView>();
  var adms = admServico.Todos(pagina);

  foreach (var adm in adms)
  {
    listAdms.Add(new AdministradorModelView
    {
      Id = adm.Id,
      Email = adm.Email,
      Perfil = adm.Perfil
    });
  }
  return Results.Ok(listAdms);
}).WithTags("Administradores");

app.MapGet("/adms/{id}", ([FromRoute] int id, IAdministradorServico admServico) =>
{
  var adm = admServico.BuscarPorId(id);

  if (adm == null)
    return Results.NotFound();

  return Results.Ok(new AdministradorModelView
  {
    Id = adm.Id,
    Email = adm.Email,
    Perfil = adm.Perfil
  });
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