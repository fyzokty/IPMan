using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using IPMan.App.Resources;
using Xunit;

namespace IPMan.Tests.Presentation;

/// <summary>Keeps every user-facing resource aligned with the UX glossary (UX-ilkeleri.md section 4).</summary>
public sealed class StringsTerminologyTests
{
    // Terms that must not reach the screen. A pattern is listed in AllowedTechnicalKeys only when the
    // technical wording is deliberate (none today: MAC, DNS and the subnet mask label are glossary terms).
    private static readonly (string Description, Regex Pattern)[] ForbiddenTerms =
    [
        ("Taslak", new Regex("taslak", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
        ("Profil", new Regex("profil", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
        ("DHCP", new Regex(@"\bDHCP\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
        ("Bağdaştırıcı", new Regex("bağdaştırıcı", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
        ("Ping", new Regex(@"\bping\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
        ("IPv4", new Regex(@"\bIPv4\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
        ("IP adresi", new Regex(@"\bIP\b", RegexOptions.CultureInvariant)),
        ("Statik", new Regex("statik", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
        ("Varsayılan ağ geçidi", new Regex("varsayılan ağ geçidi", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
        ("Yapılandırma", new Regex("yapılandırma", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
        ("Flush/Renew", new Regex(@"\b(flush|renew)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
        ("English leftovers", new Regex(
            @"\b(error|failed|success|cancel|retry|unavailable|loading|please|settings\.json|adapter|draft)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
    ];

    // Resource keys whose value may legitimately contain a forbidden term. Keep this list empty unless a
    // technical surface (diagnostic report, copied technical summary) truly needs the technical word.
    private static readonly HashSet<string> AllowedTechnicalKeys = new(StringComparer.Ordinal);

    public static IEnumerable<object[]> Entries()
    {
        ResourceManager manager = new("IPMan.App.Resources.Strings", typeof(Strings).Assembly);
        ResourceSet set = manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true)!;
        foreach (DictionaryEntry entry in set)
        {
            yield return [(string)entry.Key, (string)entry.Value!];
        }
    }

    [Theory]
    [MemberData(nameof(Entries))]
    public void Value_DoesNotUseTermsReplacedByTheGlossary(string key, string value)
    {
        if (AllowedTechnicalKeys.Contains(key))
        {
            return;
        }

        foreach ((string description, Regex pattern) in ForbiddenTerms)
        {
            Assert.False(pattern.IsMatch(value), $"{key} uses the retired term '{description}': {value}");
        }
    }

    [Fact]
    public void Glossary_TermsAreUsedWhereTheUserReadsThem()
    {
        Assert.Equal("Bilgisayarın adresi", Strings.FieldIpv4Address);
        Assert.Equal("Ağ geçidi (genelde modem adresi)", Strings.ProfileEditorGatewayLabel);
        Assert.Equal("Otomatik", Strings.ModeDhcp);
        Assert.Equal("Sabit adres", Strings.ModeStatic);
        Assert.Equal("Modeme ulaşılıyor mu?", Strings.DetailsPing);
        Assert.Equal("DNS önbelleğini temizle", Strings.DetailsFlush);
        Assert.Equal("Adresi yenile", Strings.DetailsRenew);
    }

    [Fact]
    public void AllowedTechnicalKeys_AreStillNeeded()
    {
        ResourceManager manager = new("IPMan.App.Resources.Strings", typeof(Strings).Assembly);
        foreach (string key in AllowedTechnicalKeys)
        {
            Assert.NotNull(manager.GetString(key, CultureInfo.InvariantCulture));
        }
    }
}
