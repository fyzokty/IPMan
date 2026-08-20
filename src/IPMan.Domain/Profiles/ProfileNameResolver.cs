namespace IPMan.Domain.Profiles;

/// <summary>Resolves profile display names without consulting persistent storage.</summary>
public static class ProfileNameResolver
{
    /// <summary>Returns the desired name or the next available numbered variant.</summary>
    public static string ResolveUnique(string desiredName, IEnumerable<string> existingNames)
    {
        ArgumentNullException.ThrowIfNull(desiredName);
        ArgumentNullException.ThrowIfNull(existingNames);

        HashSet<string> names = new(existingNames, StringComparer.OrdinalIgnoreCase);
        if (!names.Contains(desiredName))
        {
            return desiredName;
        }

        for (int suffix = 1; ; suffix++)
        {
            string candidate = $"{desiredName} ({suffix})";
            if (!names.Contains(candidate))
            {
                return candidate;
            }
        }
    }
}
