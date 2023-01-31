using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tgs.PromotionDistribute.FixTool.Repository.Entities;

namespace Tgs.PromotionDistribute.FixTool.Repository;

public class PromotionRepository : IPromotionRepository
{
    private readonly TgsContext _tgsContext;

    public PromotionRepository(
        TgsContext tgsContext)
    {
        _tgsContext = tgsContext ?? throw new ArgumentNullException(nameof(tgsContext));
    }

    public async ValueTask<DistributeInfo?> GetPromotionDistributeInfoAsync(
        Guid distributeId, 
        CancellationToken cancellationToken = default) 
        => await _tgsContext.PromotionDistributes
            .Where(pd => pd.Id == distributeId && pd.SentOn == null)
            .Select(pd => new DistributeInfo(
                pd.Id,
                pd.BrandId,
                pd.PlanId,
                pd.PlayerCode,
                pd.PromotionId,
                pd.BonusAmount,
                pd.Bucket,
                pd.ReferenceId,
                pd.SentOn))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    public async ValueTask UpdatePromotionDistributeAsync(
        Guid distributeId, 
        DateTime sentOn,
        CancellationToken cancellationToken = default)
    {
        var distribute = await _tgsContext.PromotionDistributes.FindAsync(
            new object?[] { distributeId },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (distribute is null)
            return;

        distribute.SentOn = sentOn.ToUniversalTime();

        _ = await _tgsContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask InsertPromotionDistributeLogAsync(PromotionDistributeLog log,
        CancellationToken cancellationToken = default)
    {
        _ = await _tgsContext.PromotionDistributeLogs.AddAsync(
            new Entities.PromotionDistributeLog
            {
                Id = log.Id,
                DistributeId = log.DistributeId,
                Action = log.Action.ToString(),
                ChangeSets = JsonSerializer.Serialize(log.ChangeSets),
                CreatedOn = DateTime.UtcNow
            },
            cancellationToken).ConfigureAwait(false);

        _ = await _tgsContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}