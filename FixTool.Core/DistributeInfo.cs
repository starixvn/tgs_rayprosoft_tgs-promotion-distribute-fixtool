namespace Tgs.PromotionDistribute.FixTool;

public record DistributeInfo(
    Guid Id,
    Guid BrandId,
    Guid? PlanId,
    string PlayerCode,
    Guid PromotionId,
    decimal BonusAmount,
    decimal BucketAmount,
    Guid? ReferenceId,
    DateTime? SentOn);