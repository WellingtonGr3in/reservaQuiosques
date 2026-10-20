using Microsoft.EntityFrameworkCore;
using Quiosques.Application.Admin;
using Quiosques.Domain.Entities;

namespace Quiosques.Infrastructure.Persistence.Repositories;

public class AdministratorRepository(QuiosquesDbContext context) : IAdministratorRepository
{
    public Task<Administrator?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        context.Administrators.AsNoTracking().SingleOrDefaultAsync(a => a.Email == email, cancellationToken);

    public void Add(Administrator administrator) => context.Administrators.Add(administrator);
}
