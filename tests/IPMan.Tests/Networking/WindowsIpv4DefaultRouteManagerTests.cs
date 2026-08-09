using System.Runtime.InteropServices;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class WindowsIpv4DefaultRouteManagerTests
{
    private static readonly Guid InterfaceGuid = new("827A2938-BB14-4D18-B67F-94E9C4F818BA");

    private static readonly NetworkAdapterId AdapterId = new(InterfaceGuid.ToString("B"));

    private static readonly WindowsInterfaceIdentity Identity = new(InterfaceGuid, 1234, 17);

    [Fact]
    public void Clear_WhenIdentityResolves_CarriesExactIdentityThroughPersistentThenActiveStore()
    {
        List<string> order = new();
        FakeIdentityResolver resolver = new(Identity);
        FakePersistentStore persistent = new(order);
        FakeActiveStore active = new(order);
        WindowsIpv4DefaultRouteManager manager = new(resolver, persistent, active);

        Ipv4DefaultRouteClearResult result = manager.Clear(AdapterId);

        Assert.True(result.IsSuccess);
        Assert.Equal(AdapterId, resolver.LastAdapterId);
        Assert.Equal(["persistent", "active"], order);
        Assert.Equal(Identity, persistent.LastIdentity);
        Assert.Equal(Identity, active.LastIdentity);
    }

    [Fact]
    public void Clear_WhenPersistentDeletionFails_DoesNotTouchActiveStore()
    {
        FakePersistentStore persistent = new()
        {
            Result = new Ipv4DefaultRouteClearResult(
                Ipv4DefaultRouteClearStatus.PersistentStoreDeleteFailed,
                5)
        };
        FakeActiveStore active = new();
        WindowsIpv4DefaultRouteManager manager = new(
            new FakeIdentityResolver(Identity),
            persistent,
            active);

        Ipv4DefaultRouteClearResult result = manager.Clear(AdapterId);

        Assert.Equal(Ipv4DefaultRouteClearStatus.PersistentStoreDeleteFailed, result.Status);
        Assert.Equal(0, active.CallCount);
    }

    [Fact]
    public void Clear_WhenIdentityDriftsAfterPersistentClear_PreventsActiveClear()
    {
        FakeIdentityResolver resolver = new(Identity);
        resolver.Revalidations.Enqueue(IdentityFailure(1168));
        FakeActiveStore active = new();
        WindowsIpv4DefaultRouteManager manager = new(
            resolver,
            new FakePersistentStore(),
            active);

        Ipv4DefaultRouteClearResult result = manager.Clear(AdapterId);

        Assert.Equal(Ipv4DefaultRouteClearStatus.InterfaceResolutionFailed, result.Status);
        Assert.Equal((uint)1168, result.TechnicalCode);
        Assert.Equal(0, active.CallCount);
    }

    [Fact]
    public void Clear_WhenIdentityDriftsAfterActiveClear_PreventsSuccess()
    {
        FakeIdentityResolver resolver = new(Identity);
        resolver.Revalidations.Enqueue(IdentitySuccess());
        resolver.Revalidations.Enqueue(IdentityFailure(1168));
        FakeActiveStore active = new();
        WindowsIpv4DefaultRouteManager manager = new(
            resolver,
            new FakePersistentStore(),
            active);

        Ipv4DefaultRouteClearResult result = manager.Clear(AdapterId);

        Assert.Equal(Ipv4DefaultRouteClearStatus.InterfaceResolutionFailed, result.Status);
        Assert.Equal((uint)1168, result.TechnicalCode);
        Assert.Equal(1, active.CallCount);
    }

    [Fact]
    public void Clear_WhenActiveDeleteReturnsNotFoundAndFinalIsEmptyWithStableIdentity_Succeeds()
    {
        FakeIdentityResolver resolver = new(Identity);
        FakeActiveRouteNativeApi native = new(Table(ExactRow()), Table())
        {
            DeleteResults = { SystemWindowsActiveRouteStore.ErrorNotFound }
        };
        WindowsIpv4DefaultRouteManager manager = new(
            resolver,
            new FakePersistentStore(),
            new SystemWindowsActiveRouteStore(native));

        Ipv4DefaultRouteClearResult result = manager.Clear(AdapterId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, resolver.RevalidateCount);
    }

    [Fact]
    public void Clear_WhenActiveDeleteReturnsNotFoundAndFinalIsEmptyWithIdentityDrift_FailsClosed()
    {
        FakeIdentityResolver resolver = new(Identity);
        resolver.Revalidations.Enqueue(IdentitySuccess());
        resolver.Revalidations.Enqueue(IdentityFailure(1168));
        FakeActiveRouteNativeApi native = new(Table(ExactRow()), Table())
        {
            DeleteResults = { SystemWindowsActiveRouteStore.ErrorNotFound }
        };
        WindowsIpv4DefaultRouteManager manager = new(
            resolver,
            new FakePersistentStore(),
            new SystemWindowsActiveRouteStore(native));

        Ipv4DefaultRouteClearResult result = manager.Clear(AdapterId);

        Assert.Equal(Ipv4DefaultRouteClearStatus.InterfaceResolutionFailed, result.Status);
        Assert.Equal((uint)1168, result.TechnicalCode);
        Assert.Single(native.DeletedRows);
    }

    [Fact]
    public void Resolve_WhenNativeConversionsSucceed_ReturnsGuidDerivedLuidAndIndex()
    {
        FakeIdentityNativeApi native = new();
        SystemWindowsInterfaceIdentityResolver resolver = new(native);

        InterfaceIdentityResolution result = resolver.Resolve(AdapterId);

        Assert.True(result.IsSuccess);
        Assert.Equal(Identity, result.Identity);
        Assert.Equal(1, native.GuidToLuidCalls);
        Assert.Equal(1, native.LuidToIndexCalls);
    }

    [Theory]
    [InlineData(87U, 0U)]
    [InlineData(0U, 1168U)]
    public void Resolve_WhenNativeConversionFails_FailsClosed(uint guidResult, uint indexResult)
    {
        FakeIdentityNativeApi native = new()
        {
            GuidToLuidResult = guidResult,
            LuidToIndexResult = indexResult
        };
        SystemWindowsInterfaceIdentityResolver resolver = new(native);

        InterfaceIdentityResolution result = resolver.Resolve(AdapterId);

        Assert.False(result.IsSuccess);
        Assert.Equal(Ipv4DefaultRouteClearStatus.InterfaceResolutionFailed, result.Status);
        Assert.Equal(guidResult == 0 ? indexResult : guidResult, result.TechnicalCode);
    }

    [Fact]
    public void Resolve_WhenAdapterIdentityIsNotGuid_FailsClosedWithoutNativeResolution()
    {
        FakeIdentityNativeApi native = new();
        SystemWindowsInterfaceIdentityResolver resolver = new(native);

        InterfaceIdentityResolution result = resolver.Resolve(new NetworkAdapterId("{FAKE-ADAPTER}"));

        Assert.False(result.IsSuccess);
        Assert.Equal(Ipv4DefaultRouteClearStatus.InvalidAdapterIdentity, result.Status);
        Assert.Equal(0, native.GuidToLuidCalls);
    }

    [Fact]
    public void Revalidate_WhenIndexNowMapsToAnotherLuid_FailsClosed()
    {
        FakeIdentityNativeApi native = new() { IndexLuid = 9999 };
        SystemWindowsInterfaceIdentityResolver resolver = new(native);

        InterfaceIdentityResolution result = resolver.Revalidate(Identity);

        Assert.False(result.IsSuccess);
        Assert.Equal(Ipv4DefaultRouteClearStatus.InterfaceResolutionFailed, result.Status);
        Assert.Equal(1, native.IndexToLuidCalls);
    }

    [Fact]
    public void Revalidate_WhenGuidNowMapsToAnotherLuid_FailsBeforeIndexChecks()
    {
        FakeIdentityNativeApi native = new() { GuidLuid = 9999 };
        SystemWindowsInterfaceIdentityResolver resolver = new(native);

        InterfaceIdentityResolution result = resolver.Revalidate(Identity);

        Assert.False(result.IsSuccess);
        Assert.Equal(Ipv4DefaultRouteClearStatus.InterfaceResolutionFailed, result.Status);
        Assert.Equal(0, native.LuidToIndexCalls);
        Assert.Equal(0, native.IndexToLuidCalls);
    }

    [Fact]
    public void Revalidate_WhenLuidNowMapsToAnotherIndex_FailsBeforeReverseIndexCheck()
    {
        FakeIdentityNativeApi native = new() { LuidIndex = 18 };
        SystemWindowsInterfaceIdentityResolver resolver = new(native);

        InterfaceIdentityResolution result = resolver.Revalidate(Identity);

        Assert.False(result.IsSuccess);
        Assert.Equal(Ipv4DefaultRouteClearStatus.InterfaceResolutionFailed, result.Status);
        Assert.Equal(0, native.IndexToLuidCalls);
    }

    [Fact]
    public void PersistentQuery_SelectsOnlyExactIpv4DefaultRouteAndPersistentStore()
    {
        string query = SystemWindowsPersistentRouteProvider.BuildExactPersistentDefaultRouteQuery(17);

        Assert.Contains("InterfaceIndex = 17", query, StringComparison.Ordinal);
        Assert.Contains("AddressFamily = 2", query, StringComparison.Ordinal);
        Assert.Contains("DestinationPrefix = '0.0.0.0/0'", query, StringComparison.Ordinal);
        Assert.Contains("Store = 0", query, StringComparison.Ordinal);
    }

    [Fact]
    public void PersistentPredicate_WhenReturnedRouteHasWrongFamily_RefusesDelete()
    {
        AssertPersistentRouteRejected(Route("wrong-family", addressFamily: 23));
    }

    [Fact]
    public void PersistentPredicate_WhenReturnedRouteHasWrongDestination_RefusesDelete()
    {
        AssertPersistentRouteRejected(Route("wrong-destination", destinationPrefix: "10.0.0.0/8"));
    }

    [Fact]
    public void PersistentPredicate_WhenReturnedRouteHasWrongStore_RefusesDelete()
    {
        AssertPersistentRouteRejected(Route("wrong-store", store: 1));
    }

    [Fact]
    public void PersistentPredicate_WhenReturnedRouteHasWrongIndex_RefusesDelete()
    {
        AssertPersistentRouteRejected(Route("wrong-index-predicate", interfaceIndex: 18));
    }

    [Fact]
    public void PersistentParser_WhenActualPropertyIsMissingOrMalformed_FailsAsReadData()
    {
        WindowsPersistentRouteReadResult[] invalidResults =
        [
            SystemWindowsPersistentRouteProvider.ParseReturnedRoute(null, 17U, 2U, "0.0.0.0/0", 0U),
            SystemWindowsPersistentRouteProvider.ParseReturnedRoute("route", null, 2U, "0.0.0.0/0", 0U),
            SystemWindowsPersistentRouteProvider.ParseReturnedRoute("route", 17U, null, "0.0.0.0/0", 0U),
            SystemWindowsPersistentRouteProvider.ParseReturnedRoute("route", 17U, 2U, null, 0U),
            SystemWindowsPersistentRouteProvider.ParseReturnedRoute("route", 17U, 2U, "0.0.0.0/0", null),
            SystemWindowsPersistentRouteProvider.ParseReturnedRoute("route", "malformed", 2U, "0.0.0.0/0", 0U)
        ];

        Assert.All(
            invalidResults,
            result =>
            {
                Assert.False(result.IsSuccess);
                Assert.Empty(result.Routes);
                Assert.Equal(SystemWindowsPersistentRouteProvider.ErrorInvalidData, result.TechnicalCode);
            });
    }

    [Fact]
    public void PersistentClear_WhenInitialEnumerationFails_ReturnsReadFailureWithoutDelete()
    {
        FakePersistentRouteProvider provider = new(
            WindowsPersistentRouteReadResult.Failure(technicalCode: 87));
        SystemWindowsPersistentRouteStore store = CreatePersistentStore(provider);

        Ipv4DefaultRouteClearResult result = store.Clear(Identity);

        Assert.Equal(Ipv4DefaultRouteClearStatus.PersistentStoreReadFailed, result.Status);
        Assert.Equal((uint)87, result.TechnicalCode);
        Assert.Empty(provider.DeletedRoutes);
    }

    [Fact]
    public void PersistentClear_WhenPostDeleteEnumerationFails_ReturnsReadFailure()
    {
        FakePersistentRouteProvider provider = new(
            Routes(Route("route-1")),
            WindowsPersistentRouteReadResult.Failure(technicalCode: 13));
        SystemWindowsPersistentRouteStore store = CreatePersistentStore(provider);

        Ipv4DefaultRouteClearResult result = store.Clear(Identity);

        Assert.Equal(Ipv4DefaultRouteClearStatus.PersistentStoreReadFailed, result.Status);
        Assert.Equal((uint)13, result.TechnicalCode);
        Assert.Single(provider.DeletedRoutes);
    }

    [Fact]
    public void PersistentClear_WhenDeletionFails_ReturnsDeleteFailure()
    {
        FakePersistentRouteProvider provider = new(
            Routes(Route("route-1")))
        {
            DeleteResults =
            {
                WindowsPersistentRouteOperationResult.Failure(technicalCode: 55)
            }
        };
        SystemWindowsPersistentRouteStore store = CreatePersistentStore(provider);

        Ipv4DefaultRouteClearResult result = store.Clear(Identity);

        Assert.Equal(Ipv4DefaultRouteClearStatus.PersistentStoreDeleteFailed, result.Status);
        Assert.Equal((uint)55, result.TechnicalCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PersistentClear_WhenProviderDeniesAccess_ReturnsAccessDenied(bool duringRead)
    {
        FakePersistentRouteProvider provider = duringRead
            ? new FakePersistentRouteProvider(
                WindowsPersistentRouteReadResult.Failure(accessDenied: true, technicalCode: 5))
            : new FakePersistentRouteProvider(Routes(Route("route-1")));

        if (!duringRead)
        {
            provider.DeleteResults.Add(
                WindowsPersistentRouteOperationResult.Failure(accessDenied: true, technicalCode: 5));
        }

        Ipv4DefaultRouteClearResult result = CreatePersistentStore(provider).Clear(Identity);

        Assert.Equal(Ipv4DefaultRouteClearStatus.AccessDenied, result.Status);
        Assert.Equal((uint)5, result.TechnicalCode);
    }

    [Fact]
    public void PersistentClear_WhenRouteRemainsAfterDelete_ReturnsRouteStillPresent()
    {
        WindowsPersistentRoute route = Route("route-1");
        FakePersistentRouteProvider provider = new(Routes(route), Routes(route));

        Ipv4DefaultRouteClearResult result = CreatePersistentStore(provider).Clear(Identity);

        Assert.Equal(Ipv4DefaultRouteClearStatus.RouteStillPresent, result.Status);
        Assert.Single(provider.DeletedRoutes);
    }

    [Fact]
    public void PersistentClear_WhenIndexLuidRevalidationFails_RefusesDelete()
    {
        FakeIdentityNativeApi native = new() { IndexLuid = 9999 };
        SystemWindowsInterfaceIdentityResolver resolver = new(native);
        FakePersistentRouteProvider provider = new(Routes(Route("route-1")));
        SystemWindowsPersistentRouteStore store = new(provider, resolver);

        Ipv4DefaultRouteClearResult result = store.Clear(Identity);

        Assert.Equal(Ipv4DefaultRouteClearStatus.InterfaceResolutionFailed, result.Status);
        Assert.Empty(provider.DeletedRoutes);
        Assert.Equal(1, native.IndexToLuidCalls);
    }

    [Fact]
    public void PersistentClear_WhenProviderReturnsAnotherIndex_RefusesDelete()
    {
        FakePersistentRouteProvider provider = new(
            Routes(Route("wrong-index", interfaceIndex: 18)));

        Ipv4DefaultRouteClearResult result = CreatePersistentStore(provider).Clear(Identity);

        Assert.Equal(Ipv4DefaultRouteClearStatus.PersistentStoreReadFailed, result.Status);
        Assert.Empty(provider.DeletedRoutes);
    }

    [Fact]
    public void PersistentClear_WithMultipleRoutes_RevalidatesBeforeEveryDelete()
    {
        FakeIdentityResolver resolver = new(Identity);
        FakePersistentRouteProvider provider = new(
            Routes(Route("route-1"), Route("route-2")),
            Routes());
        SystemWindowsPersistentRouteStore store = new(provider, resolver);

        Ipv4DefaultRouteClearResult result = store.Clear(Identity);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, resolver.RevalidateCount);
        Assert.Equal(2, provider.DeletedRoutes.Count);
    }

    [Fact]
    public void ActiveClear_WhenInitialReadFails_ReturnsReadFailure()
    {
        FakeActiveRouteNativeApi native = new(new NativeTableResponse(87));

        Ipv4DefaultRouteClearResult result = new SystemWindowsActiveRouteStore(native).Clear(Identity);

        Assert.Equal(Ipv4DefaultRouteClearStatus.ActiveStoreReadFailed, result.Status);
        Assert.Equal((uint)87, result.TechnicalCode);
        Assert.Empty(native.DeletedRows);
    }

    [Fact]
    public void ActiveClear_WhenDeleteFails_ReturnsDeleteFailure()
    {
        FakeActiveRouteNativeApi native = new(Table(ExactRow()))
        {
            DeleteResults = { 87 }
        };

        Ipv4DefaultRouteClearResult result = new SystemWindowsActiveRouteStore(native).Clear(Identity);

        Assert.Equal(Ipv4DefaultRouteClearStatus.ActiveStoreDeleteFailed, result.Status);
        Assert.Equal((uint)87, result.TechnicalCode);
    }

    [Fact]
    public void ActiveClear_WhenDeleteIsDenied_ReturnsAccessDenied()
    {
        FakeActiveRouteNativeApi native = new(Table(ExactRow()))
        {
            DeleteResults = { SystemWindowsActiveRouteStore.ErrorAccessDenied }
        };

        Ipv4DefaultRouteClearResult result = new SystemWindowsActiveRouteStore(native).Clear(Identity);

        Assert.Equal(Ipv4DefaultRouteClearStatus.AccessDenied, result.Status);
        Assert.Equal(SystemWindowsActiveRouteStore.ErrorAccessDenied, result.TechnicalCode);
    }

    [Fact]
    public void ActiveClear_WhenDeleteRacesWithNotFoundAndFinalIsEmpty_Succeeds()
    {
        FakeActiveRouteNativeApi native = new(Table(ExactRow()), Table())
        {
            DeleteResults = { SystemWindowsActiveRouteStore.ErrorNotFound }
        };

        Ipv4DefaultRouteClearResult result = new SystemWindowsActiveRouteStore(native).Clear(Identity);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ActiveClear_WhenDeleteRacesWithNotFoundAndRouteRemains_FailsVerification()
    {
        FakeActiveRouteNativeApi native = new(Table(ExactRow()), Table(ExactRow()))
        {
            DeleteResults = { SystemWindowsActiveRouteStore.ErrorNotFound }
        };

        Ipv4DefaultRouteClearResult result = new SystemWindowsActiveRouteStore(native).Clear(Identity);

        Assert.Equal(Ipv4DefaultRouteClearStatus.RouteStillPresent, result.Status);
    }

    [Fact]
    public void ActiveClear_WhenNativeDeleteReturnsSuccessButRouteRemains_FailsVerification()
    {
        FakeActiveRouteNativeApi native = new(Table(ExactRow()), Table(ExactRow()));

        Ipv4DefaultRouteClearResult result = new SystemWindowsActiveRouteStore(native).Clear(Identity);

        Assert.Equal(Ipv4DefaultRouteClearStatus.RouteStillPresent, result.Status);
    }

    [Fact]
    public void ActiveClear_WithMultipleExactRows_DeletesEveryExactRow()
    {
        FakeActiveRouteNativeApi native = new(
            Table(ExactRow(), ExactRow()),
            Table());

        Ipv4DefaultRouteClearResult result = new SystemWindowsActiveRouteStore(native).Clear(Identity);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, native.DeletedRows.Count);
    }

    [Fact]
    public void ActiveClear_WithMixedRows_DeletesOnlyExactTargetIpv4DefaultRoute()
    {
        SystemWindowsActiveRouteStore.MibIpForwardRow2[] untouched =
        [
            Row(Identity.Luid, 18, 2, 0, 0),
            Row(9999, Identity.InterfaceIndex, 2, 0, 0),
            Row(Identity.Luid, Identity.InterfaceIndex, 2, 1, 24),
            Row(Identity.Luid, Identity.InterfaceIndex, 23, 0, 0),
            Row(9999, 18, 2, 0, 0)
        ];
        FakeActiveRouteNativeApi native = new(
            Table([ExactRow(), .. untouched]),
            Table(untouched));

        Ipv4DefaultRouteClearResult result = new SystemWindowsActiveRouteStore(native).Clear(Identity);

        Assert.True(result.IsSuccess);
        Assert.Single(native.DeletedRows);
        Assert.Equal(Identity.Luid, native.DeletedRows[0].InterfaceLuid);
        Assert.Equal(Identity.InterfaceIndex, native.DeletedRows[0].InterfaceIndex);
    }

    [Theory]
    [InlineData(1234UL, 17U, (ushort)2, 0U, (byte)0, true)]
    [InlineData(1234UL, 18U, (ushort)2, 0U, (byte)0, false)]
    [InlineData(9999UL, 17U, (ushort)2, 0U, (byte)0, false)]
    [InlineData(9999UL, 18U, (ushort)2, 0U, (byte)0, false)]
    [InlineData(1234UL, 17U, (ushort)2, 1U, (byte)24, false)]
    [InlineData(1234UL, 17U, (ushort)23, 0U, (byte)0, false)]
    public void ActiveRouteSelection_MatchesBothIdentityPartsAndOnlyIpv4Default(
        ulong luid,
        uint index,
        ushort family,
        uint destination,
        byte prefixLength,
        bool expected)
    {
        Assert.Equal(
            expected,
            SystemWindowsActiveRouteStore.IsExactDefaultRoute(
                luid,
                index,
                family,
                destination,
                prefixLength,
                Identity));
    }

    [Fact]
    public void ActiveClear_ReleasesEveryNativeTableOnSuccess()
    {
        FakeActiveRouteNativeApi native = new(Table(ExactRow()), Table());

        Ipv4DefaultRouteClearResult result = new SystemWindowsActiveRouteStore(native).Clear(Identity);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, native.FreeCount);
        Assert.Equal(2, native.GetCount);
    }

    [Fact]
    public void ActiveClear_ReleasesNativeTableWhenReadReturnsFailureWithMemory()
    {
        FakeActiveRouteNativeApi native = new(new NativeTableResponse(87, [ExactRow()]));

        Ipv4DefaultRouteClearResult result = new SystemWindowsActiveRouteStore(native).Clear(Identity);

        Assert.Equal(Ipv4DefaultRouteClearStatus.ActiveStoreReadFailed, result.Status);
        Assert.Equal(1, native.FreeCount);
    }

    [Fact]
    public void NativeAbi_SockaddrInetMatchesWindowsX64Layout()
    {
        Assert.Equal(28, Marshal.SizeOf<SystemWindowsActiveRouteStore.SockaddrInet>());
        Assert.Equal(0, OffsetOf<SystemWindowsActiveRouteStore.SockaddrInet>("Family"));
        Assert.Equal(4, OffsetOf<SystemWindowsActiveRouteStore.SockaddrInet>("Ipv4Address"));
    }

    [Fact]
    public void NativeAbi_IpAddressPrefixMatchesWindowsX64Layout()
    {
        Assert.Equal(32, Marshal.SizeOf<SystemWindowsActiveRouteStore.IpAddressPrefix>());
        Assert.Equal(28, OffsetOf<SystemWindowsActiveRouteStore.IpAddressPrefix>("PrefixLength"));
    }

    [Fact]
    public void NativeAbi_MibIpForwardRow2MatchesWindowsX64Layout()
    {
        Assert.Equal(104, Marshal.SizeOf<SystemWindowsActiveRouteStore.MibIpForwardRow2>());
        Assert.Equal(0, OffsetOf<SystemWindowsActiveRouteStore.MibIpForwardRow2>("InterfaceLuid"));
        Assert.Equal(8, OffsetOf<SystemWindowsActiveRouteStore.MibIpForwardRow2>("InterfaceIndex"));
        Assert.Equal(12, OffsetOf<SystemWindowsActiveRouteStore.MibIpForwardRow2>("DestinationPrefix"));
        Assert.Equal(44, OffsetOf<SystemWindowsActiveRouteStore.MibIpForwardRow2>("NextHop"));
        Assert.Equal(84, OffsetOf<SystemWindowsActiveRouteStore.MibIpForwardRow2>("Metric"));
        Assert.Equal(100, OffsetOf<SystemWindowsActiveRouteStore.MibIpForwardRow2>("Origin"));
    }

    [Fact]
    public void NativeAbi_FirstTableRowOffsetMatchesAlignedWindowsX64Layout()
    {
        Assert.Equal(
            SystemWindowsActiveRouteStore.FirstRowOffset,
            OffsetOf<SystemWindowsActiveRouteStore.MibIpForwardTable2Header>("FirstRow"));
        Assert.Equal(112, Marshal.SizeOf<SystemWindowsActiveRouteStore.MibIpForwardTable2Header>());
    }

    [Fact]
    public void NativeAbi_CurrentProcessUsesSupportedX64Architecture()
    {
        Assert.Equal(8, IntPtr.Size);
        Assert.Equal(Architecture.X64, RuntimeInformation.ProcessArchitecture);
        Assert.True(SystemWindowsActiveRouteStore.IsSupportedArchitecture);
    }

    private static SystemWindowsPersistentRouteStore CreatePersistentStore(
        FakePersistentRouteProvider provider) =>
        new(provider, new FakeIdentityResolver(Identity));

    private static WindowsPersistentRoute Route(
        string path,
        uint? interfaceIndex = null,
        uint addressFamily = 2,
        string destinationPrefix = "0.0.0.0/0",
        uint store = 0) =>
        new(
            path,
            interfaceIndex ?? Identity.InterfaceIndex,
            addressFamily,
            destinationPrefix,
            store);

    private static void AssertPersistentRouteRejected(WindowsPersistentRoute route)
    {
        FakePersistentRouteProvider provider = new(Routes(route));

        Ipv4DefaultRouteClearResult result = CreatePersistentStore(provider).Clear(Identity);

        Assert.Equal(Ipv4DefaultRouteClearStatus.PersistentStoreReadFailed, result.Status);
        Assert.Equal(SystemWindowsPersistentRouteProvider.ErrorInvalidData, result.TechnicalCode);
        Assert.Empty(provider.DeletedRoutes);
    }

    private static InterfaceIdentityResolution IdentitySuccess() =>
        new(Ipv4DefaultRouteClearStatus.Success, Identity);

    private static InterfaceIdentityResolution IdentityFailure(uint technicalCode) =>
        new(
            Ipv4DefaultRouteClearStatus.InterfaceResolutionFailed,
            TechnicalCode: technicalCode);

    private static WindowsPersistentRouteReadResult Routes(
        params WindowsPersistentRoute[] routes) =>
        new(routes);

    private static NativeTableResponse Table(
        params SystemWindowsActiveRouteStore.MibIpForwardRow2[] rows) =>
        new(0, rows);

    private static SystemWindowsActiveRouteStore.MibIpForwardRow2 ExactRow() =>
        Row(Identity.Luid, Identity.InterfaceIndex, 2, 0, 0);

    private static SystemWindowsActiveRouteStore.MibIpForwardRow2 Row(
        ulong luid,
        uint index,
        ushort family,
        uint destination,
        byte prefixLength) =>
        new()
        {
            InterfaceLuid = luid,
            InterfaceIndex = index,
            DestinationPrefix = new SystemWindowsActiveRouteStore.IpAddressPrefix
            {
                Prefix = new SystemWindowsActiveRouteStore.SockaddrInet
                {
                    Family = family,
                    Ipv4Address = destination
                },
                PrefixLength = prefixLength
            }
        };

    private static int OffsetOf<T>(string fieldName) =>
        Marshal.OffsetOf<T>(fieldName).ToInt32();

    private sealed class FakeIdentityResolver : IWindowsInterfaceIdentityResolver
    {
        private readonly WindowsInterfaceIdentity _identity;

        public FakeIdentityResolver(WindowsInterfaceIdentity identity) => _identity = identity;

        public NetworkAdapterId? LastAdapterId { get; private set; }

        public int RevalidateCount { get; private set; }

        public InterfaceIdentityResolution Revalidation { get; set; } =
            new(Ipv4DefaultRouteClearStatus.Success, Identity);

        public Queue<InterfaceIdentityResolution> Revalidations { get; } = new();

        public InterfaceIdentityResolution Resolve(NetworkAdapterId adapterId)
        {
            LastAdapterId = adapterId;
            return new InterfaceIdentityResolution(Ipv4DefaultRouteClearStatus.Success, _identity);
        }

        public InterfaceIdentityResolution Revalidate(WindowsInterfaceIdentity identity)
        {
            RevalidateCount++;
            return Revalidations.Count > 0
                ? Revalidations.Dequeue()
                : Revalidation;
        }
    }

    private sealed class FakeIdentityNativeApi : IWindowsInterfaceIdentityNativeApi
    {
        public uint GuidToLuidResult { get; set; }

        public uint LuidToIndexResult { get; set; }

        public uint IndexToLuidResult { get; set; }

        public ulong GuidLuid { get; set; } = Identity.Luid;

        public uint LuidIndex { get; set; } = Identity.InterfaceIndex;

        public ulong IndexLuid { get; set; } = Identity.Luid;

        public int GuidToLuidCalls { get; private set; }

        public int LuidToIndexCalls { get; private set; }

        public int IndexToLuidCalls { get; private set; }

        public uint ConvertGuidToLuid(Guid interfaceGuid, out ulong interfaceLuid)
        {
            GuidToLuidCalls++;
            interfaceLuid = GuidLuid;
            return GuidToLuidResult;
        }

        public uint ConvertLuidToIndex(ulong interfaceLuid, out uint interfaceIndex)
        {
            LuidToIndexCalls++;
            interfaceIndex = LuidIndex;
            return LuidToIndexResult;
        }

        public uint ConvertIndexToLuid(uint interfaceIndex, out ulong interfaceLuid)
        {
            IndexToLuidCalls++;
            interfaceLuid = IndexLuid;
            return IndexToLuidResult;
        }
    }

    private sealed class FakePersistentRouteProvider : IWindowsPersistentRouteProvider
    {
        private readonly Queue<WindowsPersistentRouteReadResult> _reads;

        public FakePersistentRouteProvider(params WindowsPersistentRouteReadResult[] reads) =>
            _reads = new Queue<WindowsPersistentRouteReadResult>(reads);

        public List<WindowsPersistentRouteOperationResult> DeleteResults { get; } = new();

        public List<WindowsPersistentRoute> DeletedRoutes { get; } = new();

        public WindowsPersistentRouteReadResult Enumerate(WindowsInterfaceIdentity identity) =>
            _reads.Count > 0
                ? _reads.Dequeue()
                : Routes();

        public WindowsPersistentRouteOperationResult Delete(WindowsPersistentRoute route)
        {
            DeletedRoutes.Add(route);
            if (DeleteResults.Count == 0)
            {
                return WindowsPersistentRouteOperationResult.Success();
            }

            WindowsPersistentRouteOperationResult result = DeleteResults[0];
            DeleteResults.RemoveAt(0);
            return result;
        }
    }

    private sealed class FakePersistentStore : IWindowsPersistentRouteStore
    {
        private readonly List<string>? _order;

        public FakePersistentStore(List<string>? order = null) => _order = order;

        public Ipv4DefaultRouteClearResult Result { get; set; } =
            Ipv4DefaultRouteClearResult.Success();

        public WindowsInterfaceIdentity? LastIdentity { get; private set; }

        public Ipv4DefaultRouteClearResult Clear(WindowsInterfaceIdentity identity)
        {
            LastIdentity = identity;
            _order?.Add("persistent");
            return Result;
        }
    }

    private sealed class FakeActiveStore : IWindowsActiveRouteStore
    {
        private readonly List<string>? _order;

        public FakeActiveStore(List<string>? order = null) => _order = order;

        public Ipv4DefaultRouteClearResult Result { get; set; } =
            Ipv4DefaultRouteClearResult.Success();

        public int CallCount { get; private set; }

        public WindowsInterfaceIdentity? LastIdentity { get; private set; }

        public Ipv4DefaultRouteClearResult Clear(WindowsInterfaceIdentity identity)
        {
            CallCount++;
            LastIdentity = identity;
            _order?.Add("active");
            return Result;
        }
    }

    private sealed record NativeTableResponse(
        uint Result,
        SystemWindowsActiveRouteStore.MibIpForwardRow2[]? Rows = null);

    private sealed class FakeActiveRouteNativeApi : IWindowsActiveRouteNativeApi
    {
        private readonly Queue<NativeTableResponse> _responses;

        public FakeActiveRouteNativeApi(params NativeTableResponse[] responses) =>
            _responses = new Queue<NativeTableResponse>(responses);

        public List<uint> DeleteResults { get; } = new();

        public List<SystemWindowsActiveRouteStore.MibIpForwardRow2> DeletedRows { get; } = new();

        public int GetCount { get; private set; }

        public int FreeCount { get; private set; }

        public uint GetIpForwardTable2(ushort family, out IntPtr table)
        {
            GetCount++;
            NativeTableResponse response = _responses.Dequeue();
            SystemWindowsActiveRouteStore.MibIpForwardRow2[] rows = response.Rows ?? [];
            int rowSize = Marshal.SizeOf<SystemWindowsActiveRouteStore.MibIpForwardRow2>();
            table = Marshal.AllocHGlobal(
                SystemWindowsActiveRouteStore.FirstRowOffset + (rows.Length * rowSize));
            Marshal.WriteInt32(table, rows.Length);

            for (int index = 0; index < rows.Length; index++)
            {
                Marshal.StructureToPtr(
                    rows[index],
                    IntPtr.Add(
                        table,
                        SystemWindowsActiveRouteStore.FirstRowOffset + (index * rowSize)),
                    false);
            }

            return response.Result;
        }

        public uint DeleteIpForwardEntry2(
            ref SystemWindowsActiveRouteStore.MibIpForwardRow2 row)
        {
            DeletedRows.Add(row);
            if (DeleteResults.Count == 0)
            {
                return 0;
            }

            uint result = DeleteResults[0];
            DeleteResults.RemoveAt(0);
            return result;
        }

        public void FreeMibTable(IntPtr memory)
        {
            FreeCount++;
            Marshal.FreeHGlobal(memory);
        }
    }
}
