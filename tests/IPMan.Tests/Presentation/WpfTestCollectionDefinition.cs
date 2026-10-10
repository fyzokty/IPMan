using Xunit;

namespace IPMan.Tests.Presentation;

// Concurrent BAML loading and control static initialization can deadlock in WPF's shared schema context.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class WpfTestCollectionDefinition
{
    public const string Name = "WPF/STA";
}
