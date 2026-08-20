namespace IPMan.Domain.Profiles;

/// <summary>Classifies a profile document load failure.</summary>
public enum ProfileLoadFailureKind
{
    MalformedJson = 0,
    UnsupportedSchemaVersion = 1,
    InvalidContent = 2,
    ReadFailure = 3
}
