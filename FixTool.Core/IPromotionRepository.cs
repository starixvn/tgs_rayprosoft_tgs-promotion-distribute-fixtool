namespace Tgs.PromotionDistribute.FixTool;

public interface IPromotionRepository
{
    ValueTask<DistributeInfo?> GetPromotionDistributeInfoAsync(
        Guid distributeId, 
        CancellationToken cancellationToken = default);

    ValueTask UpdatePromotionDistributeAsync(
        Guid distributeId, 
        DateTime sentOn,
        CancellationToken cancellationToken = default);

    ValueTask InsertPromotionDistributeLogAsync(
        PromotionDistributeLog log, 
        CancellationToken cancellationToken = default);
}