namespace Procofa.Application.UseCases.Companies.ChangeCompanyStatus;

public sealed record ChangeCompanyStatusCommand(Guid ClientId, Guid CompanyId, bool IsActive);
