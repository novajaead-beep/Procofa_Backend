using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Findings;

namespace Procofa.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Incrementa manualmente <c>lock_version</c> en cada entidad
/// <see cref="EntityState.Modified"/> antes de <c>SaveChanges</c> — a
/// diferencia de <c>rowversion</c>/<c>xmin</c>, un concurrency token
/// <c>long</c>/<c>bigint</c> NO se autogenera por el proveedor; EF Core solo
/// se encarga de incluirlo en la cláusula <c>WHERE lock_version = @original</c>
/// del UPDATE y de lanzar <see cref="DbUpdateConcurrencyException"/> cuando 0
/// filas resultan afectadas (esto último es comportamiento nativo de
/// <c>.IsConcurrencyToken()</c>, no requiere código adicional aquí).
///
/// Aplica a las 3 entidades con <c>lock_version</c> mapeado como
/// concurrency token (Instrucción 03): <see cref="AuditCriterion"/>,
/// <see cref="Finding"/> y <see cref="Procofa.Domain.Entities.Findings.CorrectiveAction"/>.
///
/// Se registra vía <c>DbContextOptionsBuilder.AddInterceptors(...)</c> en
/// <c>AddInfrastructure</c> — instancia única sin estado propio (thread-safe
/// para reutilizar entre requests).
/// </summary>
public sealed class ConcurrencyTokenInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        IncrementLockVersions(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        IncrementLockVersions(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void IncrementLockVersions(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Modified)
            {
                continue;
            }

            // Patrón de tipo SIN variable — evita "variable declarada pero no
            // usada" (TreatWarningsAsErrors=true): el acceso a la propiedad se
            // hace vía la API de metadatos de EF (entry.Property(...)), que
            // funciona igual con setters privados, no vía el objeto CLR.
            switch (entry.Entity)
            {
                case AuditCriterion:
                    IncrementLockVersion(entry, nameof(AuditCriterion.LockVersion));
                    break;
                case Finding:
                    IncrementLockVersion(entry, nameof(Finding.LockVersion));
                    break;
                case CorrectiveAction:
                    IncrementLockVersion(entry, nameof(CorrectiveAction.LockVersion));
                    break;
            }
        }
    }

    private static void IncrementLockVersion(EntityEntry entry, string propertyName)
    {
        var property = entry.Property(propertyName);
        var currentValue = (long)property.CurrentValue!;
        property.CurrentValue = currentValue + 1;
    }
}
