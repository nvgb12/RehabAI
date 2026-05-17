using Microsoft.EntityFrameworkCore;
using RehabAI.Application.MedicalServices;
using RehabAI.Domain.Entities;
using RehabAI.Infrastructure.Database;

namespace RehabAI.Infrastructure.MedicalServices;

public sealed class EfMedicalServiceRepository(AppDbContext dbContext) : IMedicalServiceRepository
{
    public async Task<IReadOnlyList<MedicalServiceRecord>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.MedicalServices
            .Where(service => service.IsActive && !service.IsDeleted)
            .OrderBy(service => service.Name)
            .Select(service => ToRecord(service))
            .ToListAsync(cancellationToken);
    }

    public async Task<MedicalServiceRecord?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.MedicalServices
            .Where(service => service.Id == id && service.IsActive && !service.IsDeleted)
            .Select(service => ToRecord(service))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<MedicalServiceRecord> CreateAsync(
        MedicalServiceDraft draft,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var service = new MedicalService
        {
            Name = draft.Name,
            Description = draft.Description,
            DurationMinutes = draft.DurationMinutes,
            Price = draft.Price,
            Currency = draft.Currency,
            OnlinePaymentEnabled = false,
            AutoConfirmEnabled = false,
            IsActive = draft.IsActive,
            NoShowFeeEnabled = draft.NoShowFeeEnabled,
            NoShowFeeAmount = draft.NoShowFeeAmount,
            CreatedAt = now
        };

        dbContext.MedicalServices.Add(service);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToRecord(service);
    }

    public async Task<MedicalServiceRecord?> UpdateAsync(
        Guid id,
        MedicalServiceDraft draft,
        CancellationToken cancellationToken = default)
    {
        var service = await dbContext.MedicalServices
            .SingleOrDefaultAsync(service => service.Id == id && !service.IsDeleted, cancellationToken);

        if (service is null)
        {
            return null;
        }

        service.Name = draft.Name;
        service.Description = draft.Description;
        service.DurationMinutes = draft.DurationMinutes;
        service.Price = draft.Price;
        service.Currency = draft.Currency;
        service.IsActive = draft.IsActive;
        service.NoShowFeeEnabled = draft.NoShowFeeEnabled;
        service.NoShowFeeAmount = draft.NoShowFeeAmount;
        service.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToRecord(service);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await dbContext.MedicalServices
            .SingleOrDefaultAsync(service => service.Id == id && !service.IsDeleted, cancellationToken);

        if (service is null)
        {
            return false;
        }

        service.IsActive = false;
        service.IsDeleted = true;
        service.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static MedicalServiceRecord ToRecord(MedicalService service)
    {
        return new MedicalServiceRecord(
            service.Id,
            service.Name,
            service.Description,
            service.DurationMinutes,
            service.Price,
            service.Currency,
            service.IsActive,
            service.NoShowFeeEnabled,
            service.NoShowFeeAmount);
    }
}
