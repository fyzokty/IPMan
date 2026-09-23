# Project Context

IPMan: a Windows desktop tool that switches network adapter IPv4 configuration
(static IP, gateway, DNS, DHCP) and manages saved profiles. UI language is Turkish.

Project Type: Windows desktop application (WinExe, runs requireAdministrator)
Primary Language: C# (LangVersion latest, Nullable enabled, ImplicitUsings)
Framework: .NET 8 (SDK pinned 8.0.1xx via global.json), WPF for UI
State Management: MVVM with CommunityToolkit.Mvvm ([ObservableProperty], [RelayCommand])
Architecture: Four-layer solution - Domain / Application / Infrastructure / App (see architecture.md)
Navigation: Single MainWindow with adapter tabs; no navigation framework
Networking: Local adapter state only - System.Net.NetworkInformation for reads, WMI (System.Management) and Win32 IP Helper for mutation; ICMP ping for IPv4 conflict probing. No HTTP/remote APIs.
Persistence: JSON files - profiles in Documents\IPMan\Profiles\ (one file each), settings in Documents\IPMan\settings.json, recovery snapshots in %LocalAppData%\IPMan\Backup\ (ADR-015, ADR-016). Atomic writes via AtomicJsonFileWriter.
Supported Platforms: Windows 10/11 x64 only (net8.0-windows for App, Infrastructure, tests)

## Solution
- IPMan.sln: src/IPMan.Domain, src/IPMan.Application, src/IPMan.Infrastructure, src/IPMan.App,
  tests/IPMan.Tests (unit, xUnit), tests/IPMan.IntegrationTests (opt-in destructive/diagnostic).
- Central package versions in Directory.Packages.props; TreatWarningsAsErrors + AnalysisLevel
  latest-recommended in Directory.Build.props - every analyzer warning breaks the build.
- CI: GitHub Actions windows-latest, restore -> Release build -> dotnet test on push/PR to main.

## Documentation
- Current state and next steps: docs/STATE.md. Backlog: docs/backlog/ (Turkish, one file per item).
- Requirements PR-xxx: docs/01_Requirements/; acceptance AC-xxx: docs/09_Acceptance_Criteria.md.
- Architecture docs: docs/02_Architecture/; decisions: docs/ADR/ (ADR-001..016). Respect ADRs.

## Important Constraints

- NEVER run network-mutating commands or the app's apply paths on the development machine.
- Never set IPMAN_* environment variables and never run tools/host-lab; destructive
  integration tests are opt-in and only for an isolated VM adapter.
- Preserve existing public contracts unless the task explicitly requires a change.
- Do not change architecture without an approved spec; a new architectural decision needs an ADR.
- Prefer existing project patterns and dependencies; add packages only through Directory.Packages.props.
- All user-facing text goes through Strings.resx / Strings.cs (Turkish); no literals in ViewModels or Views.
