using System.Runtime.InteropServices;
using IPMan.Infrastructure.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class WindowsAdapterMediumReaderTests
{
    private static readonly Guid InterfaceGuid = new("11111111-2222-3333-4444-555555555555");
    private static readonly Guid ForeignGuid = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");

    [Fact]
    public void MibIfRow2_OnX64_MatchesNativeLayout()
    {
        Assert.Equal(8, IntPtr.Size);
        Assert.Equal(1352, Marshal.SizeOf<WindowsAdapterMediumReader.MibIfRow2>());
        Assert.Equal(12, Offset(nameof(WindowsAdapterMediumReader.MibIfRow2.InterfaceGuid)));
        Assert.Equal(28, Offset(nameof(WindowsAdapterMediumReader.MibIfRow2.Alias)));
        Assert.Equal(542, Offset(nameof(WindowsAdapterMediumReader.MibIfRow2.Description)));
        Assert.Equal(1060, Offset(nameof(WindowsAdapterMediumReader.MibIfRow2.PhysicalAddress)));
        Assert.Equal(1092, Offset(nameof(WindowsAdapterMediumReader.MibIfRow2.PermanentPhysicalAddress)));
        Assert.Equal(1140, Offset(nameof(WindowsAdapterMediumReader.MibIfRow2.PhysicalMediumType)));
        Assert.Equal(1152, Offset(nameof(WindowsAdapterMediumReader.MibIfRow2.InterfaceAndOperStatusFlags)));
        Assert.Equal(1156, Offset(nameof(WindowsAdapterMediumReader.MibIfRow2.OperStatus)));
        Assert.Equal(1192, Offset(nameof(WindowsAdapterMediumReader.MibIfRow2.TransmitLinkSpeed)));
        Assert.Equal(8, Marshal.OffsetOf<WindowsAdapterMediumReader.MibIfTable2>(
            nameof(WindowsAdapterMediumReader.MibIfTable2.Table)).ToInt32());
    }

    [Fact]
    public void Read_WithMultipleRows_MatchesBracedAdapterGuidAndFreesTable()
    {
        using FakeGetIfTable2NativeApi native = new((ForeignGuid, 14), (InterfaceGuid, 10));

        IReadOnlyDictionary<Guid, uint> media = new WindowsAdapterMediumReader(native).Read();

        Assert.Equal(2, media.Count);
        Assert.Equal(10u, WindowsAdapterMediumReader.GetMedium(media, InterfaceGuid.ToString("B")));
        Assert.Equal(14u, WindowsAdapterMediumReader.GetMedium(media, ForeignGuid.ToString("B")));
        Assert.Equal(0u, WindowsAdapterMediumReader.GetMedium(media, Guid.NewGuid().ToString("B")));
        Assert.Equal(0u, WindowsAdapterMediumReader.GetMedium(media, "invalid-id"));
        Assert.Equal(1, native.FreeCalls);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(87u)]
    public void Read_WhenTableIsNull_ReturnsUnknownWithoutFreeingNull(uint result)
    {
        using FakeGetIfTable2NativeApi native = new() { ReturnNull = true, Result = result };

        Assert.Empty(new WindowsAdapterMediumReader(native).Read());
        Assert.Equal(0, native.FreeCalls);
    }

    [Fact]
    public void Read_WhenNativeCallFails_ReturnsUnknownAndFreesTable()
    {
        using FakeGetIfTable2NativeApi native = new((InterfaceGuid, 10)) { Result = 87 };

        Assert.Empty(new WindowsAdapterMediumReader(native).Read());
        Assert.Equal(1, native.FreeCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Read_WhenNativeCallThrows_ReturnsUnknownAndFreesTable(int failure)
    {
        using FakeGetIfTable2NativeApi native = new()
        {
            GetException = failure switch
            {
                0 => new DllNotFoundException(),
                1 => new EntryPointNotFoundException(),
                _ => new InvalidOperationException()
            }
        };

        Assert.Empty(new WindowsAdapterMediumReader(native).Read());
        Assert.Equal(1, native.FreeCalls);
    }

    [Fact]
    public void Read_WhenFreeThrows_ReturnsUnknownWithoutFailingDiscovery()
    {
        using FakeGetIfTable2NativeApi native = new((InterfaceGuid, 10))
        {
            FreeException = new InvalidOperationException()
        };

        Assert.Empty(new WindowsAdapterMediumReader(native).Read());
        Assert.Equal(1, native.FreeCalls);
    }

    [Theory]
    [InlineData(65_537u)]
    [InlineData(uint.MaxValue)]
    public void Read_WhenCountIsInvalid_ReturnsUnknownAndFreesTable(uint count)
    {
        using FakeGetIfTable2NativeApi native = new();
        native.SetCount(count);

        Assert.Empty(new WindowsAdapterMediumReader(native).Read());
        Assert.Equal(1, native.FreeCalls);
    }

    [Theory]
    [InlineData(0u, 0u, 0u)]
    [InlineData(0u, 10u, 10u)]
    [InlineData(10u, 0u, 10u)]
    [InlineData(10u, 14u, 10u)]
    [InlineData(14u, 10u, 14u)]
    public void Read_WhenGuidIsDuplicated_KeepsFirstNonZeroMediumAndOtherRows(
        uint firstMedium, uint laterMedium, uint expected)
    {
        Guid trailingGuid = new("BBBBBBBB-CCCC-DDDD-EEEE-FFFFFFFFFFFF");
        using FakeGetIfTable2NativeApi native = new(
            (ForeignGuid, 14),
            (InterfaceGuid, firstMedium),
            (InterfaceGuid, laterMedium),
            (InterfaceGuid, 0),
            (trailingGuid, 8));

        IReadOnlyDictionary<Guid, uint> media = new WindowsAdapterMediumReader(native).Read();

        Assert.Equal(3, media.Count);
        Assert.Equal(expected, media[InterfaceGuid]);
        Assert.Equal(14u, media[ForeignGuid]);
        Assert.Equal(8u, media[trailingGuid]);
        Assert.Equal(1, native.FreeCalls);
    }

    [Fact]
    public void Read_WhenGuidIsEmpty_SkipsRowAndPreservesOtherRows()
    {
        using FakeGetIfTable2NativeApi native = new(
            (ForeignGuid, 14), (Guid.Empty, 10), (InterfaceGuid, 8));

        IReadOnlyDictionary<Guid, uint> media = new WindowsAdapterMediumReader(native).Read();

        Assert.Equal(2, media.Count);
        Assert.False(media.ContainsKey(Guid.Empty));
        Assert.Equal(14u, media[ForeignGuid]);
        Assert.Equal(8u, media[InterfaceGuid]);
        Assert.Equal(1, native.FreeCalls);
    }

    [Fact]
    public void Read_WhenTableIsEmpty_ReturnsUnknownAndFreesTable()
    {
        using FakeGetIfTable2NativeApi native = new();

        Assert.Empty(new WindowsAdapterMediumReader(native).Read());
        Assert.Equal(1, native.FreeCalls);
    }

    private static int Offset(string field) =>
        Marshal.OffsetOf<WindowsAdapterMediumReader.MibIfRow2>(field).ToInt32();
}
