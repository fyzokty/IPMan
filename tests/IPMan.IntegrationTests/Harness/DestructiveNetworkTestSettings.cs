using IPMan.Domain.Networking;

namespace IPMan.IntegrationTests.Harness;

public sealed record DestructiveNetworkTestSettings(
    NetworkAdapterId AdapterId,
    Sprint08Scenario Scenario,
    StaticIpv4Configuration DesiredConfiguration,
    string EvidenceRoot);
