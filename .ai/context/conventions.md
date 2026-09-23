# Project Conventions

## Naming
- File-scoped namespaces matching folder: IPMan.<Layer>.<Feature> (Networking, Profiles, Settings, Common).
- Interfaces `I<Capability>`; one coherent capability per interface (no god services).
- Operation results are `<Operation>Result` records with a `<Operation>Status` enum
  (e.g. DhcpApplyResult / DhcpApplyStatus); options classes `<Service>Options`.
- Private fields `_camelCase`; Windows-specific implementations prefixed `Windows`/`Wmi`/`System`.
- Test methods: `Method_WhenCondition_ExpectedResult` (CA1707 disabled for tests only).

## File Structure
- One public type per file, file named after the type.
- Feature folders inside each layer: Networking/, Profiles/, Settings/, Common/.
- App: ViewModels/, Views/ (XAML + minimal code-behind), Presentation/ (formatters, UI service
  abstractions and their Wpf* implementations), Resources/ (Strings.resx + typed Strings.cs).

## State Management
- ViewModels derive from ObservableObject and use CommunityToolkit source generators
  ([ObservableProperty], [NotifyCanExecuteChangedFor], [RelayCommand]); classes are `sealed partial`.
- UI-thread marshalling only through IUiDispatcher; user confirmations through
  IUserConfirmationService; clipboard through IClipboardService.
- Action outcomes are shown inline (StatusMessage + ApplyStatusSeverity), never as modal dialogs.

## Error Handling
- Constructors guard every dependency with ArgumentNullException.ThrowIfNull and validate options.
- Expected failures are returned as status values in result types, not thrown.
- Infrastructure isolates storage/WMI failures and maps them to statuses; malformed profile
  files are isolated, not fatal.

## Logging
- ILogger<T> (Microsoft.Extensions.Logging.Abstractions) is injected; currently wired to
  NullLogger in App.ConfigureServices. No logging provider yet (backlog 07).

## Testing
- xUnit 2.9 in tests/IPMan.Tests; mirror the source folder (Networking/, Profiles/, ViewModels/...).
- No mocking library: hand-written fakes in tests/IPMan.Tests/Fakes/ (Fake<Interface>); reuse
  or extend them. Shared builders in TestData.cs.
- Tests are deterministic: use IClock, IDelayProvider (ImmediateDelayProvider), FakeUiDispatcher.
- Unit tests must never touch real adapters, WMI, or user folders (use temp directories for
  file-system tests).
- tests/IPMan.IntegrationTests are skipped unless IPMAN_* gates are set; never add tests there
  that run without the gates.

## Imports
- System directives first (dotnet_sort_system_directives_first); ImplicitUsings is on.

## Formatting
- .editorconfig: 4 spaces for .cs/.xaml, 2 for md/json/yml; UTF-8; final newline; Allman braces
  (csharp_new_line_before_open_brace = all); explicit accessibility modifiers.

## Code Style
- XML doc `<summary>` on public types and members, as in existing code.
- Comment non-obvious code.
- Do not compress logic into unreadable one-liners.
- Use explicit types in declarations as the existing code does (e.g. `DhcpApplyResult result = ...`).
- No business or network logic in XAML code-behind.
