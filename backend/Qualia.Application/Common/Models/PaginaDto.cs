namespace Qualia.Application.Common.Models;

/// Listado paginado
public sealed record PaginaDto<T>(IReadOnlyList<T> Items, int Pagina, int TamanoPagina, int Total);
