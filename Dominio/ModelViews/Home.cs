namespace MinimalApi.Dominio.ModelViews;

public struct Home
{
  public string Mensagem { get => "Bem vindo a Minimal API"; }
  public string Doc { get => "/swagger"; }
}