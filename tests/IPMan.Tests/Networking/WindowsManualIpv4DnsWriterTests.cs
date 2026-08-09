using System.Runtime.InteropServices;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class WindowsManualIpv4DnsWriterTests
{
    private const ulong DnsSettingIpv6 = 0x0001;

    private static readonly Guid InterfaceGuid =
        new("827A2938-BB14-4D18-B67F-94E9C4F818BA");

    private static readonly NetworkAdapterId AdapterId = new(InterfaceGuid.ToString("B"));

    [Fact]
    public void Write_WithExactGuid_ForwardsExactGuidToNativeApi()
    {
        FakeDnsWriterNativeApi native = new();

        new WindowsManualIpv4DnsWriter(native).Write(AdapterId, ["1.1.1.1"]);

        Assert.Equal(InterfaceGuid, native.InterfaceId);
        Assert.Equal(1, native.CallCount);
    }

    [Fact]
    public void Write_WithOneServer_ProducesExactNameServerPayload()
    {
        FakeDnsWriterNativeApi native = new();

        new WindowsManualIpv4DnsWriter(native).Write(AdapterId, ["1.1.1.1"]);

        Assert.Equal("1.1.1.1", native.NameServer);
    }

    [Fact]
    public void Write_WithTwoServers_PreservesExactCallerOrder()
    {
        FakeDnsWriterNativeApi native = new();

        new WindowsManualIpv4DnsWriter(native).Write(
            AdapterId,
            ["8.8.8.8", "1.1.1.1"]);

        Assert.Equal("8.8.8.8,1.1.1.1", native.NameServer);
    }

    [Fact]
    public void Write_RequestIncludesOnlyDnsSettingNameServer()
    {
        FakeDnsWriterNativeApi native = new();

        new WindowsManualIpv4DnsWriter(native).Write(AdapterId, ["1.1.1.1"]);

        Assert.Equal(WindowsManualIpv4DnsWriter.DnsSettingNameServer, native.Settings.Flags);
    }

    [Fact]
    public void Write_RequestDoesNotIncludeDnsSettingIpv6()
    {
        FakeDnsWriterNativeApi native = new();

        new WindowsManualIpv4DnsWriter(native).Write(AdapterId, ["1.1.1.1"]);

        Assert.Equal(
            0UL,
            native.Settings.Flags & DnsSettingIpv6);
    }

    [Fact]
    public void Write_RequestDoesNotPopulateUnrelatedDnsSettings()
    {
        FakeDnsWriterNativeApi native = new();

        new WindowsManualIpv4DnsWriter(native).Write(AdapterId, ["1.1.1.1"]);

        Assert.Equal((uint)1, native.Settings.Version);
        Assert.Equal(IntPtr.Zero, native.Settings.Domain);
        Assert.Equal(IntPtr.Zero, native.Settings.SearchList);
        Assert.Equal((uint)0, native.Settings.RegistrationEnabled);
        Assert.Equal((uint)0, native.Settings.RegisterAdapterName);
        Assert.Equal((uint)0, native.Settings.EnableLlmnr);
        Assert.Equal((uint)0, native.Settings.QueryAdapterName);
        Assert.Equal(IntPtr.Zero, native.Settings.ProfileNameServer);
    }

    [Fact]
    public void Write_WhenNativeReturnsZero_ReturnsSuccess()
    {
        ManualIpv4DnsWriteResult result = new WindowsManualIpv4DnsWriter(
            new FakeDnsWriterNativeApi()).Write(AdapterId, ["1.1.1.1"]);

        Assert.True(result.IsSuccess);
        Assert.Equal(ManualIpv4DnsWriteStatus.Success, result.Status);
        Assert.Equal((uint)0, result.TechnicalCode);
    }

    [Fact]
    public void Write_WhenNativeReturnsFailure_RetainsTechnicalCode()
    {
        ManualIpv4DnsWriteResult result = new WindowsManualIpv4DnsWriter(
            new FakeDnsWriterNativeApi(87)).Write(AdapterId, ["1.1.1.1"]);

        Assert.False(result.IsSuccess);
        Assert.Equal(ManualIpv4DnsWriteStatus.NativeCallFailed, result.Status);
        Assert.Equal((uint)87, result.TechnicalCode);
    }

    [Fact]
    public void Write_WhenAdapterIdentityIsNotGuid_DoesNotCallNativeApi()
    {
        FakeDnsWriterNativeApi native = new();

        ManualIpv4DnsWriteResult result = new WindowsManualIpv4DnsWriter(native).Write(
            new NetworkAdapterId("not-a-guid"),
            ["1.1.1.1"]);

        Assert.Equal(ManualIpv4DnsWriteStatus.InvalidAdapterIdentity, result.Status);
        Assert.Equal(0, native.CallCount);
    }

    [Theory]
    [InlineData()]
    [InlineData("not-an-ip")]
    [InlineData("2001:4860:4860::8888")]
    [InlineData("1.1.1.1", "8.8.8.8", "9.9.9.9")]
    public void Write_WhenServerListIsUnsupported_DoesNotCallNativeApi(params string[] servers)
    {
        FakeDnsWriterNativeApi native = new();

        ManualIpv4DnsWriteResult result = new WindowsManualIpv4DnsWriter(native).Write(
            AdapterId,
            servers);

        Assert.Equal(ManualIpv4DnsWriteStatus.InvalidServerList, result.Status);
        Assert.Equal(0, native.CallCount);
    }

    private sealed class FakeDnsWriterNativeApi : IDnsInterfaceSettingsWriterNativeApi
    {
        private readonly uint _result;

        public FakeDnsWriterNativeApi(uint result = 0) => _result = result;

        public int CallCount { get; private set; }

        public Guid? InterfaceId { get; private set; }

        public string? NameServer { get; private set; }

        public DnsInterfaceSettingsV1 Settings { get; private set; }

        public uint Set(Guid interfaceId, ref DnsInterfaceSettingsV1 settings)
        {
            CallCount++;
            InterfaceId = interfaceId;
            NameServer = Marshal.PtrToStringUni(settings.NameServer);
            Settings = settings;
            return _result;
        }
    }
}
