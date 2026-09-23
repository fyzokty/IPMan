# Architecture Context

## Layer Boundaries
- IPMan.Domain (net8.0): pure model - adapter snapshots, Ipv4 values, mutation plans,
  recovery snapshots, NetworkProfile, AppSettings. No I/O, no dependencies.
- IPMan.Application (net8.0): use-case services and ports (interfaces) - apply services
  (StaticIpv4ApplyService, DhcpApplyService, RecoveryRestoreService), preflight, validator,
  comparer, AdapterRefreshCoordinator, ProfileCatalog, repository interfaces.
- IPMan.Infrastructure (net8.0-windows): Windows implementations - WMI configurator and
  readers, IP Helper route/DNS access, NetworkInterface probe, JSON repositories and codecs,
  file watcher, named mutexes, named-pipe activation, single-instance guard.
- IPMan.App (WPF WinExe): composition root (App.xaml.cs), ViewModels, Views, Presentation, Resources.

## Dependency Direction
- Domain <- Application <- Infrastructure <- App (App may reference all). Application never
  references Infrastructure or WPF (docs/02_Architecture/02_Solution_Dependency_Rules.md).
- System.Management and profile/settings file-system access only in Infrastructure.
- No WPF Dispatcher use in Domain/Application.

## Feature Structure
- Networking: read (INetworkAdapterReader) -> validate/preflight -> recovery snapshot capture ->
  one mutation serialized by INetworkMutationCoordinator (in-process + Global\ mutex) ->
  bounded verification. Every mutation writes a recovery snapshot first (ADR-016).
- Refresh: NetworkChangeMonitor events -> debounced AdapterRefreshCoordinator -> ViewModels (ADR-010).
- Profiles: JsonProfileRepository + FileSystemProfileDirectoryWatcher -> ProfileCatalog
  (load, debounced reload, malformed-file isolation). No profile ViewModel yet (backlog 01).
- Settings: JsonAppSettingsRepository over AppSettings (additive fields, no schema bump).

## Global State
- Single process, single instance (Local\ mutex + admin-only named pipe activation, ADR-011).
- Whole app elevated via app.manifest requireAdministrator (ADR-013); IElevationStateProvider
  gates mutation. Services are DI singletons.

## Repository / Data Boundaries
- Repository interfaces in Application; JSON implementations and codecs in Infrastructure.
- Storage paths come from AppStorageLayout, passed through *RepositoryOptions.
- Writes are atomic (AtomicJsonFileWriter). JSON persistence per ADR-003.

## Navigation
- One MainWindow; MainWindowViewModel owns adapter tabs (AdapterViewModel with details and
  draft); AdapterActionsViewModel runs apply/DHCP/restore for the selected tab.

## Dependency Injection
- Microsoft.Extensions.DependencyInjection; everything registered in App.ConfigureServices
  as singletons; provider built with ValidateOnBuild and ValidateScopes. Register new
  services there.

## Error Handling
- Results with status enums across Application boundaries; ViewModels map them to Turkish
  inline messages through formatters in App/Presentation (ApplyResultMessageFormatter,
  ValidationMessageFormatter). Details: docs/02_Architecture/08_Error_Handling_and_Logging.md.
