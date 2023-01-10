using Microsoft.EntityFrameworkCore;
using Tgs.PromotionDistribute.FixTool;
using Tgs.PromotionDistribute.FixTool.Repository;
using Tgs.PromotionDistribute.FixTool.Repository.Entities;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFixToolRepository(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder>? dbOptionsAction = null)
        => services
            .AddDbContext<TgsContext>(dbOptionsAction)
            .AddTransient<IPromotionRepository, PromotionRepository>();
}