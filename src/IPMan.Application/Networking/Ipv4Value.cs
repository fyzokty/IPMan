using System.Globalization;
using System.Net;

namespace IPMan.Application.Networking;

internal readonly record struct Ipv4Value(uint Bits, string Text)
{
    public static bool TryParse(string? input, out Ipv4Value value)
    {
        value = default;
        string text = input?.Trim() ?? string.Empty;
        string[] parts = text.Split('.');

        if (parts.Length != 4)
        {
            return false;
        }

        byte[] bytes = new byte[4];

        for (int index = 0; index < parts.Length; index++)
        {
            if (!byte.TryParse(
                    parts[index],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out bytes[index]))
            {
                return false;
            }
        }

        IPAddress address = new(bytes);
        value = new Ipv4Value(ToBits(bytes), address.ToString());
        return true;
    }

    public bool IsUnspecified => Bits == 0;

    public bool IsLimitedBroadcast => Bits == uint.MaxValue;

    public bool IsLoopback => (Bits & 0xff000000) == 0x7f000000;

    public bool IsMulticast => (Bits & 0xf0000000) == 0xe0000000;

    private static uint ToBits(IReadOnlyList<byte> bytes) =>
        ((uint)bytes[0] << 24) |
        ((uint)bytes[1] << 16) |
        ((uint)bytes[2] << 8) |
        bytes[3];
}
