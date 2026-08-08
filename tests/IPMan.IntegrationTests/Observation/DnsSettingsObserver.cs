using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using IPMan.Domain.Networking;

namespace IPMan.IntegrationTests.Observation;

/// <summary>Read-only DNS settings observer using the highest documented platform version.</summary>
public sealed class DnsSettingsObserver
{
    private const uint NoError = 0;
    private const int Version2MinimumBuild = 19041;
    private const int Version3MinimumBuild = 19645;

    public static DnsSettingsObservation Read(NetworkAdapterId adapterId)
    {
        if (!Guid.TryParse(adapterId.Value, out Guid interfaceId))
        {
            return Unsupported();
        }

        try
        {
            return ReadForPlatform(
                Environment.OSVersion.Version,
                version => ReadNative(interfaceId, version));
        }
        catch (DllNotFoundException)
        {
            return InvocationFailedForSelectedPlatform();
        }
        catch (EntryPointNotFoundException)
        {
            return InvocationFailedForSelectedPlatform();
        }
    }

    internal static DnsSettingsObservation ReadForPlatform(
        Version platformVersion,
        Func<int, DnsSettingsObservation> readVersion)
    {
        ArgumentNullException.ThrowIfNull(platformVersion);
        ArgumentNullException.ThrowIfNull(readVersion);
        int? selectedVersion = SelectHighestSupportedVersion(platformVersion);

        return selectedVersion.HasValue
            ? readVersion(selectedVersion.Value)
            : Unsupported();
    }

    internal static int? SelectHighestSupportedVersion(Version platformVersion)
    {
        ArgumentNullException.ThrowIfNull(platformVersion);

        if (platformVersion.Major != 10)
        {
            return null;
        }

        return platformVersion.Build switch
        {
            >= Version3MinimumBuild => 3,
            >= Version2MinimumBuild => 2,
            _ => null
        };
    }

    private static DnsSettingsObservation ReadNative(Guid interfaceId, int version) =>
        version switch
        {
            3 => ReadV3(interfaceId),
            2 => ReadV2(interfaceId),
            1 => ReadV1(interfaceId),
            _ => throw new ArgumentOutOfRangeException(nameof(version))
        };

    private static DnsSettingsObservation ReadV3(Guid interfaceId)
    {
        DnsInterfaceSettings3 settings = new() { Version = 3 };
        uint result = GetInterfaceDnsSettingsV3(interfaceId, ref settings);

        if (result != NoError)
        {
            return Failed(result, version: 3);
        }

        try
        {
            return Create(
                version: 3,
                settings.Flags,
                settings.NameServer,
                settings.ProfileNameServer,
                settings.SupplementalSearchList,
                ReadServerProperties(settings.ServerProperties, settings.ServerPropertyCount),
                ReadServerProperties(settings.ProfileServerProperties, settings.ProfileServerPropertyCount));
        }
        finally
        {
            FreeInterfaceDnsSettingsV3(ref settings);
        }
    }

    private static DnsSettingsObservation ReadV2(Guid interfaceId)
    {
        DnsInterfaceSettings2 settings = new() { Version = 2 };
        uint result = GetInterfaceDnsSettingsV2(interfaceId, ref settings);

        if (result != NoError)
        {
            return Failed(result, version: 2);
        }

        try
        {
            return Create(
                version: 2,
                settings.Flags,
                settings.NameServer,
                settings.ProfileNameServer,
                settings.SupplementalSearchList,
                Array.Empty<DnsServerPropertyObservation>(),
                Array.Empty<DnsServerPropertyObservation>());
        }
        finally
        {
            FreeInterfaceDnsSettingsV2(ref settings);
        }
    }

    private static DnsSettingsObservation ReadV1(Guid interfaceId)
    {
        DnsInterfaceSettings1 settings = new() { Version = 1 };
        uint result = GetInterfaceDnsSettingsV1(interfaceId, ref settings);

        if (result != NoError)
        {
            return Failed(result, version: 1);
        }

        try
        {
            return Create(
                version: 1,
                settings.Flags,
                settings.NameServer,
                settings.ProfileNameServer,
                IntPtr.Zero,
                Array.Empty<DnsServerPropertyObservation>(),
                Array.Empty<DnsServerPropertyObservation>());
        }
        finally
        {
            FreeInterfaceDnsSettingsV1(ref settings);
        }
    }

    private static DnsSettingsObservation Create(
        int version,
        ulong flags,
        IntPtr nameServer,
        IntPtr profileNameServer,
        IntPtr supplementalSearchList,
        IReadOnlyList<DnsServerPropertyObservation> serverProperties,
        IReadOnlyList<DnsServerPropertyObservation> profileServerProperties)
    {
        string? supplemental = Marshal.PtrToStringUni(supplementalSearchList);
        uint[] unsupportedPropertyTypes = serverProperties
            .Concat(profileServerProperties)
            .Where(property => !property.PayloadSupported)
            .Select(property => property.Type)
            .Distinct()
            .OrderBy(type => type)
            .ToArray();
        return new DnsSettingsObservation(
            Supported: true,
            NativeError: null,
            version,
            flags,
            Marshal.PtrToStringUni(nameServer),
            Marshal.PtrToStringUni(profileNameServer),
            SupplementalSearchListPresent: !string.IsNullOrEmpty(supplemental),
            SupplementalSearchListHash: HashSensitive(supplemental),
            RicherPropertiesStatus: version < 3
                ? DnsRicherPropertiesObservationStatus.NotApplicableByPlatform
                : unsupportedPropertyTypes.Length == 0
                    ? DnsRicherPropertiesObservationStatus.Complete
                    : DnsRicherPropertiesObservationStatus.Incomplete,
            unsupportedPropertyTypes,
            serverProperties,
            profileServerProperties);
    }

    private static DnsServerPropertyObservation[] ReadServerProperties(IntPtr pointer, uint count)
    {
        if (pointer == IntPtr.Zero || count == 0)
        {
            return Array.Empty<DnsServerPropertyObservation>();
        }

        int size = Marshal.SizeOf<DnsServerPropertyNative>();
        DnsServerPropertyObservation[] properties = new DnsServerPropertyObservation[count];

        for (int index = 0; index < count; index++)
        {
            DnsServerPropertyNative property = Marshal.PtrToStructure<DnsServerPropertyNative>(
                IntPtr.Add(pointer, checked(index * size)));
            bool payloadSupported = property.Version == 1 &&
                property.Type == 1 &&
                property.Property != IntPtr.Zero;
            DnsDohServerSettings? doh = payloadSupported
                ? Marshal.PtrToStructure<DnsDohServerSettings>(property.Property)
                : null;
            string? template = doh is null ? null : Marshal.PtrToStringUni(doh.Value.Template);
            properties[index] = new DnsServerPropertyObservation(
                property.Version,
                property.ServerIndex,
                property.Type,
                payloadSupported,
                doh?.Flags,
                DohTemplatePresent: !string.IsNullOrEmpty(template),
                DohTemplateHash: HashSensitive(template));
        }

        return properties;
    }

    private static string? HashSensitive(string? value) =>
        string.IsNullOrEmpty(value)
            ? null
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static DnsSettingsObservation Failed(uint result, int version) =>
        new(
            Supported: true,
            NativeError: result,
            Version: version,
            Flags: 0,
            NameServer: null,
            ProfileNameServer: null,
            SupplementalSearchListPresent: false,
            SupplementalSearchListHash: null,
            RicherPropertiesStatus: DnsRicherPropertiesObservationStatus.Incomplete,
            Array.Empty<uint>(),
            Array.Empty<DnsServerPropertyObservation>(),
            Array.Empty<DnsServerPropertyObservation>());

    private static DnsSettingsObservation Unsupported() =>
        new(
            Supported: false,
            NativeError: null,
            Version: null,
            Flags: 0,
            NameServer: null,
            ProfileNameServer: null,
            SupplementalSearchListPresent: false,
            SupplementalSearchListHash: null,
            RicherPropertiesStatus: DnsRicherPropertiesObservationStatus.NotApplicableByPlatform,
            Array.Empty<uint>(),
            Array.Empty<DnsServerPropertyObservation>(),
            Array.Empty<DnsServerPropertyObservation>());

    private static DnsSettingsObservation InvocationFailedForSelectedPlatform()
    {
        int? selectedVersion = SelectHighestSupportedVersion(Environment.OSVersion.Version);

        return selectedVersion.HasValue
            ? new DnsSettingsObservation(
                Supported: true,
                NativeError: null,
                Version: selectedVersion.Value,
                Flags: 0,
                NameServer: null,
                ProfileNameServer: null,
                SupplementalSearchListPresent: false,
                SupplementalSearchListHash: null,
                RicherPropertiesStatus: DnsRicherPropertiesObservationStatus.Incomplete,
                Array.Empty<uint>(),
                Array.Empty<DnsServerPropertyObservation>(),
                Array.Empty<DnsServerPropertyObservation>())
            : Unsupported();
    }

    [DllImport("iphlpapi.dll", EntryPoint = "GetInterfaceDnsSettings", ExactSpelling = true)]
    private static extern uint GetInterfaceDnsSettingsV1(Guid interfaceId, ref DnsInterfaceSettings1 settings);

    [DllImport("iphlpapi.dll", EntryPoint = "GetInterfaceDnsSettings", ExactSpelling = true)]
    private static extern uint GetInterfaceDnsSettingsV2(Guid interfaceId, ref DnsInterfaceSettings2 settings);

    [DllImport("iphlpapi.dll", EntryPoint = "GetInterfaceDnsSettings", ExactSpelling = true)]
    private static extern uint GetInterfaceDnsSettingsV3(Guid interfaceId, ref DnsInterfaceSettings3 settings);

    [DllImport("iphlpapi.dll", EntryPoint = "FreeInterfaceDnsSettings", ExactSpelling = true)]
    private static extern void FreeInterfaceDnsSettingsV1(ref DnsInterfaceSettings1 settings);

    [DllImport("iphlpapi.dll", EntryPoint = "FreeInterfaceDnsSettings", ExactSpelling = true)]
    private static extern void FreeInterfaceDnsSettingsV2(ref DnsInterfaceSettings2 settings);

    [DllImport("iphlpapi.dll", EntryPoint = "FreeInterfaceDnsSettings", ExactSpelling = true)]
    private static extern void FreeInterfaceDnsSettingsV3(ref DnsInterfaceSettings3 settings);

    [StructLayout(LayoutKind.Sequential)]
    private struct DnsInterfaceSettings1
    {
        public uint Version;
        public ulong Flags;
        public IntPtr Domain;
        public IntPtr NameServer;
        public IntPtr SearchList;
        public uint RegistrationEnabled;
        public uint RegisterAdapterName;
        public uint EnableLlmnr;
        public uint QueryAdapterName;
        public IntPtr ProfileNameServer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DnsInterfaceSettings2
    {
        public uint Version;
        public ulong Flags;
        public IntPtr Domain;
        public IntPtr NameServer;
        public IntPtr SearchList;
        public uint RegistrationEnabled;
        public uint RegisterAdapterName;
        public uint EnableLlmnr;
        public uint QueryAdapterName;
        public IntPtr ProfileNameServer;
        public uint DisableUnconstrainedQueries;
        public IntPtr SupplementalSearchList;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DnsInterfaceSettings3
    {
        public uint Version;
        public ulong Flags;
        public IntPtr Domain;
        public IntPtr NameServer;
        public IntPtr SearchList;
        public uint RegistrationEnabled;
        public uint RegisterAdapterName;
        public uint EnableLlmnr;
        public uint QueryAdapterName;
        public IntPtr ProfileNameServer;
        public uint DisableUnconstrainedQueries;
        public IntPtr SupplementalSearchList;
        public uint ServerPropertyCount;
        public IntPtr ServerProperties;
        public uint ProfileServerPropertyCount;
        public IntPtr ProfileServerProperties;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DnsServerPropertyNative
    {
        public uint Version;
        public uint ServerIndex;
        public uint Type;
        public IntPtr Property;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DnsDohServerSettings
    {
        public IntPtr Template;
        public ulong Flags;
    }
}
