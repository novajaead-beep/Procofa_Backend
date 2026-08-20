namespace Procofa.Domain.Common;

/// <summary>
/// Excepción de dominio: representa la violación de una regla de negocio invariante.
/// Los adaptadores (API REST) la traducen a HTTP 422/409 según corresponda; nunca se filtra
/// como excepción técnica genérica hacia el exterior del núcleo hexagonal.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
