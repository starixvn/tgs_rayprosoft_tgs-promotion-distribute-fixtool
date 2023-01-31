using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Patch;
using Tgs.CashService.Client;
using Tgs.PlayerService.Client;
using Tgs.SerialNumberGenerator;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace Tgs.PromotionDistribute.FixTool.ApplicationServices;

internal class ManualDistributeBonusByIdQueryService : IGetService<ManualDistributeBonusByIdQuery, DistributeResult?>
{
    private readonly IPlayerServiceClient _playerServiceClient;
    private readonly ICashServiceClient _cashServiceClient;
    private readonly IPlayerBucketClient _playerBucketClient;
    private readonly IPromotionRepository _promotionRepository;
    private readonly ISerialNumberGenerator _serialNumberGenerator;

    private static readonly JsonSerializerOptions _SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Converters = { new JsonStringEnumConverter() }
    };

    public ManualDistributeBonusByIdQueryService(
        IPlayerServiceClient playerServiceClient,
        ICashServiceClient cashServiceClient,
        IPlayerBucketClient playerBucketClient,
        IPromotionRepository promotionRepository,
        ISerialNumberGenerator serialNumberGenerator)
    {
        _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
        _cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
        _playerBucketClient = playerBucketClient ?? throw new ArgumentNullException(nameof(playerBucketClient));
        _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
        _serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
    }
    
    public async ValueTask<DistributeResult?> GetAsync(
        ManualDistributeBonusByIdQuery query, 
        CancellationToken cancellationToken = default)
    {
        var distributeInfo =
            await _promotionRepository.GetPromotionDistributeInfoAsync(query.DistributeId, cancellationToken)
                .ConfigureAwait(false);

        if (distributeInfo is null)
            return null;

        var player = await _playerServiceClient.GetPlayerInfoAsync(
            distributeInfo.PlayerCode,
            cancellationToken).ConfigureAwait(false);

        if (player is null)
            throw new InvalidOperationException($"Player with player code {distributeInfo.PlayerCode} does not exist");

        var playerProperties = await _playerServiceClient.GetPlayerPropertiesAsync(
            player.PlayerCode,
            cancellationToken).ConfigureAwait(false);

        if (playerProperties is null)
            throw new InvalidOperationException($"Player with player code {distributeInfo.PlayerCode} does not exist");

        var walletId = Guid.Parse(playerProperties.GetValueOrDefault("DefaultWalletId")!);
        
        var bucketId = await _playerBucketClient.CreateBucketAsync(
            player.PlayerCode,
            distributeInfo.BucketAmount,
            BucketType.Promotion,
            distributeInfo.Id.ToString(),
            cancellationToken).ConfigureAwait(false);

        var transactionResponse = await _cashServiceClient.PromotionAsync(
            walletId,
            distributeInfo.BonusAmount,
            null,
            distributeInfo.Id.ToString(),
            null,
            cancellationToken).ConfigureAwait(false);

        var updateDistributeInfo = distributeInfo with
        {
            SentOn = DateTime.UtcNow
        };

        await _promotionRepository.UpdatePromotionDistributeAsync(
            updateDistributeInfo.Id,
            updateDistributeInfo.SentOn.Value,
            cancellationToken).ConfigureAwait(false);
        
        await _promotionRepository.InsertPromotionDistributeLogAsync(
            new PromotionDistributeLog(
                await _serialNumberGenerator
                    .GenerateAsync(cancellationToken)
                    .ConfigureAwait(false),
                distributeInfo.Id,
                DistributeAction.Distribute,
                JsonSerializer.SerializeToDocument(distributeInfo.CreatePatch(
                    updateDistributeInfo,
                    _SerializerOptions))),
            cancellationToken).ConfigureAwait(false);

        return new DistributeResult(
            bucketId,
            transactionResponse.TransactionId);
    }
}