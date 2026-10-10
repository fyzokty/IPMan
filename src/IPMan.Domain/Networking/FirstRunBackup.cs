namespace IPMan.Domain.Networking;

/// <summary>A single, independent copy of the visible adapters' initial state.</summary>
public sealed record FirstRunBackup(
    int SchemaVersion,
    DateTimeOffset CapturedAtUtc,
    RecoverySnapshot[] Adapters)
{
    public const int CurrentSchemaVersion = 1;
}
