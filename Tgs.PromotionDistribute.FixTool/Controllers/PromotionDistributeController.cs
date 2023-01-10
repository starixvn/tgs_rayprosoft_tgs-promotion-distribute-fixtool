using Microsoft.AspNetCore.Mvc;

namespace Tgs.PromotionDistribute.FixTool.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PromotionDistributeController : ControllerBase
{
    [HttpGet]
    public ValueTask<Guid> GetIdAsync()
        => ValueTask.FromResult(Guid.NewGuid());
}