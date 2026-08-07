using System.Globalization;
using System.Management;

namespace IPMan.Infrastructure.Networking;

internal sealed class SystemWmiNetworkAdapterSessionFactory : IWmiNetworkAdapterSessionFactory
{
    public WmiAdapterResolution ResolveBySettingId(string adapterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterId);

        string query = BuildExactIdentityQuery(adapterId);

        using ManagementObjectSearcher searcher = new(@"root\CIMV2", query);
        using ManagementObjectCollection results = searcher.Get();
        List<ManagementObject> exactMatches = new();

        foreach (ManagementObject candidate in results)
        {
            string? settingId = candidate["SettingID"] as string;

            if (string.Equals(settingId, adapterId, StringComparison.OrdinalIgnoreCase))
            {
                exactMatches.Add(candidate);
            }
            else
            {
                candidate.Dispose();
            }
        }

        if (exactMatches.Count == 1)
        {
            return new WmiAdapterResolution(
                WmiAdapterResolutionStatus.Found,
                new SystemWmiNetworkAdapterSession(exactMatches[0]));
        }

        foreach (ManagementObject match in exactMatches)
        {
            match.Dispose();
        }

        return new WmiAdapterResolution(
            exactMatches.Count == 0
                ? WmiAdapterResolutionStatus.Unavailable
                : WmiAdapterResolutionStatus.Ambiguous);
    }

    internal static string BuildExactIdentityQuery(string adapterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterId);
        string escapedId = adapterId.Replace("'", "''", StringComparison.Ordinal);
        return "SELECT * FROM Win32_NetworkAdapterConfiguration " +
            $"WHERE IPEnabled = TRUE AND SettingID = '{escapedId}'";
    }

    private sealed class SystemWmiNetworkAdapterSession : IWmiNetworkAdapterSession
    {
        private readonly ManagementObject _configuration;

        public SystemWmiNetworkAdapterSession(ManagementObject configuration) =>
            _configuration = configuration;

        public uint Invoke(
            string methodName,
            IReadOnlyDictionary<string, object?>? parameters)
        {
            ManagementBaseObject? input = null;

            try
            {
                if (parameters is not null)
                {
                    input = _configuration.GetMethodParameters(methodName);

                    foreach ((string name, object? value) in parameters)
                    {
                        input[name] = value;
                    }
                }

                using ManagementBaseObject output =
                    _configuration.InvokeMethod(methodName, input, null) ??
                    throw new ManagementException($"WMI method {methodName} returned no result.");

                return Convert.ToUInt32(output["ReturnValue"], CultureInfo.InvariantCulture);
            }
            finally
            {
                input?.Dispose();
            }
        }

        public object? ReadProperty(string propertyName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
            return _configuration[propertyName];
        }

        public void Dispose() => _configuration.Dispose();
    }
}
