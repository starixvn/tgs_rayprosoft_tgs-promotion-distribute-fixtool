using System.Collections.Immutable;
using NSubstitute;
using Tgs.CashService.Client;
using Tgs.PlayerService.Client;
using Tgs.PromotionDistribute.FixTool;
using Tgs.PromotionDistribute.FixTool.ApplicationServices;
using Tgs.SerialNumberGenerator;
using Tgs.Service.PlayerBuckets;

namespace FixTool.Core.UnitTests;

public class DistributeTests
{
	[Fact]
	public async Task 以DistributeId單筆手動派發()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

		var playerInfo = new PlayerInfo
		{
			PlayerCode = "player01",
			BrandId = Guid.NewGuid(),
			UserName = "player01"
		};

		var walletId = Guid.NewGuid();
		var playerProperties = new Dictionary<string, string>
		{
			["DefaultWalletId"] = walletId.ToString(),
		};

		var distributeInfo = new DistributeInfo(
			Guid.NewGuid(),
			playerInfo.BrandId,
			Guid.NewGuid(),
			playerInfo.PlayerCode,
			Guid.NewGuid(),
			100m,
			500m,
			null,
			null);

		var bucketId = Guid.NewGuid();
		var transactionId = Guid.NewGuid();

		var query = new ManualDistributeBonusByIdQuery(distributeInfo.Id);

		var sut = new ManualDistributeBonusByIdQueryService(
			fakePlayerServiceClient,
			fakeCashServiceClient,
			fakePlayerBucketClient,
			fakePromotionRepository,
			fakeSerialNumberGenerator);

		_ = fakePromotionRepository.GetPromotionDistributeInfoAsync(
				Arg.Is(distributeInfo.Id),
				Arg.Any<CancellationToken>())
			.Returns(distributeInfo);

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(
				Arg.Is(playerInfo.PlayerCode),
				Arg.Any<CancellationToken>())
			.Returns(playerInfo);

		_ = fakePlayerServiceClient.GetPlayerPropertiesAsync(
				Arg.Is(playerInfo.PlayerCode),
				Arg.Any<CancellationToken>())
			.Returns(playerProperties.ToImmutableDictionary());
		
		_ = fakePlayerBucketClient.CreateBucketAsync(
				Arg.Is(playerInfo.PlayerCode),
				Arg.Is(distributeInfo.BucketAmount),
				Arg.Is(BucketType.Promotion),
				Arg.Is(distributeInfo.Id.ToString()),
				Arg.Any<CancellationToken>())
			.Returns(bucketId);

		_ = fakeCashServiceClient.PromotionAsync(
				Arg.Is(walletId),
				Arg.Is(distributeInfo.BonusAmount),
				Arg.Any<string?>(),
				Arg.Is(distributeInfo.Id.ToString()),
				Arg.Any<Guid?>(),
				Arg.Any<CancellationToken>())
			.Returns(new TransactionResponse
			{
				TransactionId = transactionId
			});

		var expected = new DistributeResult(bucketId, transactionId);

		var actual = await sut.GetAsync(query);

		Assert.Equal(expected, actual);
		
		_ = fakePromotionRepository
			.Received(1)
			.UpdatePromotionDistributeAsync(
				Arg.Is(distributeInfo.Id),
				Arg.Any<DateTime>(),
				Arg.Any<CancellationToken>());

		_ = fakePromotionRepository
			.Received(1)
			.InsertPromotionDistributeLogAsync(
				Arg.Is<PromotionDistributeLog>(x =>
					x.DistributeId == distributeInfo.Id
					&& x.Action == DistributeAction.Distribute),
				Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task 當DistributeInfo找不到時不處理該筆手動派發()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

		var distributeId = Guid.NewGuid();
		
		var query = new ManualDistributeBonusByIdQuery(distributeId);

		var sut = new ManualDistributeBonusByIdQueryService(
			fakePlayerServiceClient,
			fakeCashServiceClient,
			fakePlayerBucketClient,
			fakePromotionRepository,
			fakeSerialNumberGenerator);

		_ = fakePromotionRepository.GetPromotionDistributeInfoAsync(
				Arg.Is(distributeId),
				Arg.Any<CancellationToken>())
			.Returns((DistributeInfo?)null);
		
		var actual = await sut.GetAsync(query);

		Assert.Null(actual);

		_ = fakePlayerServiceClient
			.DidNotReceive()
			.GetPlayerInfoAsync(
				Arg.Any<string>(),
				Arg.Any<CancellationToken>());

		_ = fakePlayerServiceClient
			.DidNotReceive()
			.GetPlayerPropertiesAsync(
				Arg.Any<string>(),
				Arg.Any<CancellationToken>());

		_ = fakePlayerBucketClient
			.DidNotReceive()
			.CreateBucketAsync(
			Arg.Any<string>(),
			Arg.Any<decimal>(),
			Arg.Is(BucketType.Promotion),
			Arg.Is(distributeId.ToString()),
			Arg.Any<CancellationToken>());

		_ = fakeCashServiceClient
			.DidNotReceive()
			.PromotionAsync(
				Arg.Any<Guid>(),
				Arg.Any<decimal>(),
				Arg.Any<string?>(),
				Arg.Is(distributeId.ToString()),
				Arg.Any<Guid?>(),
				Arg.Any<CancellationToken>());

		_ = fakePromotionRepository
			.DidNotReceive()
			.UpdatePromotionDistributeAsync(
				Arg.Is(distributeId),
				Arg.Any<DateTime>(),
				Arg.Any<CancellationToken>());

		_ = fakePromotionRepository
			.DidNotReceive()
			.InsertPromotionDistributeLogAsync(
				Arg.Is<PromotionDistributeLog>(x =>
					x.DistributeId == distributeId
					&& x.Action == DistributeAction.Distribute),
				Arg.Any<CancellationToken>());
	}
}