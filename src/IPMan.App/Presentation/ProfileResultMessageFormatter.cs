using IPMan.App.Resources;
using IPMan.Application.Profiles;
using IPMan.Domain.Profiles;

namespace IPMan.App.Presentation;

/// <summary>Translates profile storage outcomes to localized inline text.</summary>
public static class ProfileResultMessageFormatter
{
    /// <summary>Describes a save result.</summary>
    public static string Describe(ProfileSaveStatus status) => status switch
    {
        ProfileSaveStatus.Success => Strings.ProfileSaveSuccess,
        ProfileSaveStatus.InvalidContent => Strings.ProfileInvalidContent,
        ProfileSaveStatus.AccessDenied => Strings.ProfileAccessDenied,
        ProfileSaveStatus.IoFailure => Strings.ProfileIoFailure,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    /// <summary>Describes a delete result.</summary>
    public static string Describe(ProfileDeleteStatus status) => status switch
    {
        ProfileDeleteStatus.Success => Strings.ProfileDeleteSuccess,
        ProfileDeleteStatus.NotFound => Strings.ProfileNotFound,
        ProfileDeleteStatus.AccessDenied => Strings.ProfileAccessDenied,
        ProfileDeleteStatus.IoFailure => Strings.ProfileIoFailure,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    /// <summary>Describes an import result.</summary>
    public static string Describe(ProfileImportResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Status == ProfileImportStatus.Success && result.Profile is not null
            ? Strings.FormatProfileImportSuccess(result.Profile.Name)
            : Describe(result.Status);
    }

    /// <summary>Describes an import status.</summary>
    public static string Describe(ProfileImportStatus status) => status switch
    {
        ProfileImportStatus.Success => Strings.ProfileImportSuccess,
        ProfileImportStatus.InvalidContent => Strings.ProfileInvalidContent,
        ProfileImportStatus.AccessDenied => Strings.ProfileAccessDenied,
        ProfileImportStatus.IoFailure => Strings.ProfileIoFailure,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    /// <summary>Describes an export result.</summary>
    public static string Describe(ProfileExportStatus status) => status switch
    {
        ProfileExportStatus.Success => Strings.ProfileExportSuccess,
        ProfileExportStatus.NotFound => Strings.ProfileNotFound,
        ProfileExportStatus.AccessDenied => Strings.ProfileAccessDenied,
        ProfileExportStatus.IoFailure => Strings.ProfileIoFailure,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    /// <summary>Describes a malformed profile document problem.</summary>
    public static string Describe(ProfileLoadFailureKind kind) => kind switch
    {
        ProfileLoadFailureKind.MalformedJson => Strings.ProfileProblemMalformedJson,
        ProfileLoadFailureKind.UnsupportedSchemaVersion => Strings.ProfileProblemUnsupportedSchema,
        ProfileLoadFailureKind.InvalidContent => Strings.ProfileProblemInvalidContent,
        ProfileLoadFailureKind.ReadFailure => Strings.ProfileProblemReadFailure,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}
