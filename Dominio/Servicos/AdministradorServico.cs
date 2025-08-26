using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.DTOs;
using MinimalApi.Infraestrutura.Db;

namespace MinimalApi.Dominio.Servicos;

public class AdministradorServico : IAdministradorServico
{
  private readonly DbContexto _contexto;

  public AdministradorServico(DbContexto contexto)
  {
    _contexto = contexto;
  }

  public Administrador? Login(LoginDTO loginDTO)
  {
    return _contexto.Administradores
      .Where(a => a.Email == loginDTO.Email && a.Senha == loginDTO.Senha)
      .FirstOrDefault();
  }

  public Administrador Incluir(Administrador adm)
  {
    _contexto.Administradores.Add(adm);
    _contexto.SaveChanges();

    return adm;
  }

  public Administrador? BuscarPorId(int id)
  {
    return _contexto.Administradores.Where(adm => adm.Id == id).FirstOrDefault(); ;
  }

  public List<Administrador> Todos(int? pagina)
  {
    var query = _contexto.Administradores.AsQueryable();
    int itensPorPagia = 10;

    if (pagina != null)
      query = query.Skip(((int)pagina - 1) * itensPorPagia).Take(itensPorPagia);

    return query.ToList();
  }
}