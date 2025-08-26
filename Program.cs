using MinimalApi.Infraestrutura.Db;
using MinimalApi.DTOs;
using Microsoft.EntityFrameworkCore;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.Dominio.Servicos;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Dominio.ModelViews;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.OpenApi.Models;

#region Builder
var builder = WebApplication.CreateBuilder(args);
var key = builder.Configuration.GetSection("Jwt").ToString();

builder.Services.AddAuthentication(option =>
{
  option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option =>
{
  option.TokenValidationParameters = new TokenValidationParameters
  {
    ValidateLifetime = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key ?? "11223344")),
    ValidateIssuer = false,
    ValidateAudience = false
  };
});
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
  options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
  {
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "Insira o seu token JWT"
  });

  options.AddSecurityRequirement(new OpenApiSecurityRequirement
  {
    {
      new OpenApiSecurityScheme{
        Reference = new OpenApiReference{
          Type = ReferenceType.SecurityScheme,
          Id ="Bearer"
        }
      },
      new string[] {}
    }
  });
});
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
}).AllowAnonymous().WithTags("Home");
#endregion

#region Administradores

string GerarTokenJwt(Administrador adm)
{
  if (string.IsNullOrEmpty(key)) return string.Empty;

  var chaveSegura = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
  var credenciais = new SigningCredentials(chaveSegura, SecurityAlgorithms.HmacSha256);
  var claims = new List<Claim>()
  {
    new Claim("Email", adm.Email),
    new Claim("Perfil", adm.Perfil),
  };
  var token = new JwtSecurityToken(
    claims: claims,
    expires: DateTime.Now.AddDays(1),
    signingCredentials: credenciais
  );

  return new JwtSecurityTokenHandler().WriteToken(token);
}

app.MapPost("/adm/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico admServico) =>
{
  var adm = admServico.Login(loginDTO);

  if (adm != null)
  {
    string token = GerarTokenJwt(adm);
    return Results.Ok(new AdministradorLogado
    {
      Email = adm.Email,
      Perfil = adm.Perfil,
      Token = token
    });
  }

  else
    return Results.Unauthorized();
}).AllowAnonymous().WithTags("Administradores");

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
}).RequireAuthorization().WithTags("Administradores");

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
}).RequireAuthorization().WithTags("Administradores");
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
).RequireAuthorization().WithTags("Veículos");

app.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculoServico veiculoServico) =>
{
  var veiculos = veiculoServico.Todos(pagina);

  return Results.Ok(veiculos);
}).RequireAuthorization().WithTags("Veículos");

app.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
{
  var veiculo = veiculoServico.BuscarPorId(id);

  if (veiculo == null)
    return Results.NotFound();
  else
    return Results.Ok(veiculo);
}).RequireAuthorization().WithTags("Veículos");

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
}).RequireAuthorization().WithTags("Veículos");

app.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
{
  var veiculo = veiculoServico.BuscarPorId(id);

  if (veiculo == null)
    return Results.NotFound();

  veiculoServico.Apagar(veiculo);

  return Results.NoContent();
}).RequireAuthorization().WithTags("Veículos");

#endregion

#region App
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
#endregion