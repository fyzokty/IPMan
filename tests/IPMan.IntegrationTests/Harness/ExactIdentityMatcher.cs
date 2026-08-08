namespace IPMan.IntegrationTests.Harness;

public static class ExactIdentityMatcher
{
    public static T? FindUnique<T>(
        IEnumerable<T> candidates,
        Func<T, string> identitySelector,
        string exactIdentity)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(identitySelector);
        ArgumentException.ThrowIfNullOrWhiteSpace(exactIdentity);

        T[] matches = candidates
            .Where(candidate => string.Equals(
                identitySelector(candidate),
                exactIdentity,
                StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();

        return matches.Length == 1 ? matches[0] : null;
    }
}
