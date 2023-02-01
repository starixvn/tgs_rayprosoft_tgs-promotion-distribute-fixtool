using Microsoft.AspNetCore.Mvc;
using Tgs.PromotionDistribute.FixTool.ApplicationServices;
using Tgs.PromotionDistribute.FixTool.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.PromotionDistribute.FixTool.Controllers;

[Route("api/distribute")]
[ApiController]
public class PromotionDistributeController : ControllerBase
{
    [HttpPost("manual/{distributeId}")]
    public async ValueTask<DistributeResultViewModel?> ManualDistributeBonusByIdAsync(
        [FromServices] IGetService<ManualDistributeBonusByIdQuery, DistributeResult?> getService,
        Guid distributeId)
    {
        var result = await getService.GetAsync(
                new ManualDistributeBonusByIdQuery(distributeId),
                HttpContext.RequestAborted)
            .ConfigureAwait(false);

        return result is null ? null : new DistributeResultViewModel
        {
			BucketId = result.BucketId,
			TransactionId = result.TransactionId
        };
    }
}