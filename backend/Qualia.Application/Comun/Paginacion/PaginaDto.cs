namespace Qualia.Application.Comun.Paginacion;

/// <summary>Listado paginado</summary>
public sealed record PaginaDto<T>(IReadOnlyList<T> Items, int Pagina, int TamanoPagina, int Total);
