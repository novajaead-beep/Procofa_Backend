namespace Procofa.Domain.Entities.Catalogs;

/// <summary>
/// Perfil de empresa auditada (MAQUILA, TRANSPORTISTA, AGENTE_ADUANAL, 3PL,
/// SOCIO_COMERCIAL, OTRO). Catálogo global sin <c>tenant_id</c>, sin RLS.
/// Tabla física: <c>profiles</c>, 6 filas sembradas. Mismo régimen de GRANTs
/// que <see cref="Program"/> (decisión congelada #9).
/// </summary>
public sealed class Profile
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Profile() { }

    public Profile(Guid id, string code, string name, string? description)
    {
        Id = id;
        Code = code;
        Name = name;
        Description = description;
        IsActive = true;
    }
}
