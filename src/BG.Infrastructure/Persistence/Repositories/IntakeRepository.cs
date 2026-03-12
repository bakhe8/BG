using BG.Application.Contracts.Persistence;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Operations;
using Microsoft.EntityFrameworkCore;

namespace BG.Infrastructure.Persistence.Repositories;

internal sealed class IntakeRepository : IIntakeRepository
{
    private readonly BgDbContext _dbContext;

    public IntakeRepository(BgDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<User>> ListIntakeActorsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .ThenInclude(role => role.RolePermissions)
            .Where(user => user.IsActive &&
                           user.UserRoles.Any(userRole =>
                               userRole.Role.RolePermissions.Any(rolePermission =>
                                   rolePermission.PermissionKey == "intake.view" ||
                                   rolePermission.PermissionKey == "intake.verify" ||
                                   rolePermission.PermissionKey == "intake.finalize")))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetIntakeActorByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .ThenInclude(role => role.RolePermissions)
            .SingleOrDefaultAsync(
                user => user.Id == userId &&
                        user.IsActive &&
                        user.UserRoles.Any(userRole =>
                            userRole.Role.RolePermissions.Any(rolePermission =>
                                rolePermission.PermissionKey == "intake.view" ||
                                rolePermission.PermissionKey == "intake.verify" ||
                                rolePermission.PermissionKey == "intake.finalize")),
                cancellationToken);
    }

    public Task<bool> GuaranteeNumberExistsAsync(string guaranteeNumber, CancellationToken cancellationToken = default)
    {
        return _dbContext.Guarantees.AnyAsync(
            guarantee => guarantee.GuaranteeNumber == guaranteeNumber,
            cancellationToken);
    }

    public Task<Guarantee?> GetGuaranteeByNumberAsync(string guaranteeNumber, CancellationToken cancellationToken = default)
    {
        return _dbContext.Guarantees
            .Include(guarantee => guarantee.Documents)
            .Include(guarantee => guarantee.Requests)
            .Include(guarantee => guarantee.Correspondence)
            .Include(guarantee => guarantee.Events)
            .SingleOrDefaultAsync(
                guarantee => guarantee.GuaranteeNumber == guaranteeNumber,
                cancellationToken);
    }

    public Task AddGuaranteeAsync(Guarantee guarantee, CancellationToken cancellationToken = default)
    {
        return _dbContext.Guarantees.AddAsync(guarantee, cancellationToken).AsTask();
    }

    public Task AddOperationsReviewItemAsync(OperationsReviewItem reviewItem, CancellationToken cancellationToken = default)
    {
        return _dbContext.OperationsReviewItems.AddAsync(reviewItem, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
