using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IPMan.Application.Networking;
using IPMan.IntegrationTests.Observation;

namespace IPMan.IntegrationTests.Evidence;

public sealed class EvidenceWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _directory;

    public EvidenceWriter(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = Path.GetFullPath(directory);
    }

    public string DirectoryPath => _directory;

    public Task WriteBeforeAsync(
        Sprint08BeforeEvidence evidence,
        CancellationToken cancellationToken) =>
        WriteAtomicAsync("before.json", evidence, cancellationToken);

    public async Task WriteResultAsync(
        Sprint08ResultEvidence evidence,
        CancellationToken cancellationToken)
    {
        await WriteAtomicAsync("result.json", evidence, cancellationToken).ConfigureAwait(false);
        Sprint08SanitizedSummary sanitized = CreateSanitizedSummary(evidence);
        await WriteAtomicAsync("summary.sanitized.json", sanitized, cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteAtomicAsync<T>(
        string fileName,
        T value,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_directory);
        string finalPath = Path.Combine(_directory, fileName);
        string temporaryPath = Path.Combine(_directory, $".{fileName}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (FileStream stream = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(
                        stream,
                        value,
                        SerializerOptions,
                        cancellationToken)
                    .ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryPath, finalPath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    private static Sprint08SanitizedSummary CreateSanitizedSummary(Sprint08ResultEvidence evidence)
    {
        StaticIpv4ApplyResult? result = evidence.ApplyResult;
        DnsSettingsObservation? afterDns = evidence.AfterObservation?.DnsSettings;
        bool richerDnsObserved = evidence.Before.Observation.DnsSettings.ServerProperties.Count > 0 ||
            evidence.Before.Observation.DnsSettings.ProfileServerProperties.Count > 0 ||
            (afterDns?.ServerProperties.Count ?? 0) > 0 ||
            (afterDns?.ProfileServerProperties.Count ?? 0) > 0;
        bool richerDnsComplete =
            evidence.Before.Observation.DnsSettings.RicherPropertiesStatus ==
                DnsRicherPropertiesObservationStatus.Complete &&
            afterDns?.RicherPropertiesStatus == DnsRicherPropertiesObservationStatus.Complete;
        bool unsupportedRicherDns = evidence.Before.Observation.DnsSettings.UnsupportedPropertyTypes.Count > 0 ||
            (afterDns?.UnsupportedPropertyTypes.Count ?? 0) > 0;
        bool hasFailure = evidence.Failure is not null;

        return new Sprint08SanitizedSummary(
            evidence.Before.RunId,
            evidence.Before.StartedAtUtc,
            evidence.CompletedAtUtc,
            evidence.Before.Scenario,
            evidence.Before.RequestedDimensions,
            Hash(evidence.Before.ExactAdapterId.Value),
            result?.Status.ToString(),
            result?.Mutation?.Ipv4Step.TechnicalCode,
            result?.Mutation?.GatewayStep.TechnicalCode,
            result?.Mutation?.DnsStep.TechnicalCode,
            result?.Recovery?.SnapshotId,
            evidence.RecoveryMatchesBeforeState,
            evidence.Before.Observation.Ipv6Enabled,
            evidence.Before.Observation.Ipv6Routes.Complete &&
                (evidence.AfterObservation?.Ipv6Routes.Complete ?? false),
            richerDnsObserved,
            richerDnsComplete,
            unsupportedRicherDns,
            evidence.Passed,
            evidence.Differences.Count,
            hasFailure ? "SCENARIO_EXCEPTION" : null,
            hasFailure ? "Scenario execution failed; inspect local raw evidence." : null);
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
