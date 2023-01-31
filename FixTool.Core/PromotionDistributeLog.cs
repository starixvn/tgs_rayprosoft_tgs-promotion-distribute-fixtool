using System.Text.Json;

namespace Tgs.PromotionDistribute.FixTool;

public record PromotionDistributeLog(Guid Id, Guid DistributeId, DistributeAction Action, JsonDocument ChangeSets);