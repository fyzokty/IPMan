using Xunit;

namespace IPMan.IntegrationTests.Harness;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class DestructiveNetworkFactAttribute : FactAttribute
{
    public DestructiveNetworkFactAttribute()
    {
        DestructiveNetworkTestSettingsResult parsed = DestructiveNetworkTestSettingsParser.Parse(
            DestructiveNetworkTestEnvironment.Read(),
            AppContext.BaseDirectory);
        Skip = DestructiveTestExecutionGate.GetSkipReason(parsed);
    }
}
