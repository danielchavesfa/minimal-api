using MinimalApi.Dominio.Entidades;
using MinimalApi.DTOs;

namespace MinimalApi.Dominio.Interfaces;

public interface IAdministradorServico
{
  Administrador? Login(LoginDTO loginDTO);
  Administrador Incluir(Administrador adm);
  Administrador? BuscarPorId(int id);
  List<Administrador> Todos(int? pagina);
}