using System.Windows;
using System.Windows.Threading;
using IPMan.App.Presentation;
using IPMan.App.ViewModels;
using IPMan.App.Views;
using IPMan.Application.Common;
using IPMan.Application.Networking;
using IPMan.Application.Profiles;
using IPMan.Application.Settings;
using IPMan.Infrastructure.Common;
using IPMan.Infrastructure.Networking;
using IPMan.Infrastructure.Profiles;
using IPMan.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("IPMan.Tests")]

namespace IPMan.App;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "WPF application lifetime releases process resources in OnExit.")]
public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;
    private WindowsSingleInstanceGuard? _singleInstanceGuard;
    private IActivationChannelServer? _activationChannelServer;
    private MainWindowActivationHandler? _activationHandler;
    private IProfileCatalog? _profileCatalog;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceGuard = new WindowsSingleInstanceGuard();
        IActivationChannelClient activationClient = new NamedPipeActivationChannelClient(
            new SystemDelayProvider(),
            new ActivationChannelOptions());
        SingleInstanceStartupCoordinator startupCoordinator = new(
            _singleInstanceGuard,
            activationClient,
            AllowExistingInstanceToSetForegroundWindow);

        SingleInstanceStartupOutcome startupOutcome = startupCoordinator
            .CoordinateAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        if (startupOutcome != SingleInstanceStartupOutcome.PrimaryInstance)
        {
            if (startupOutcome == SingleInstanceStartupOutcome.ActivationFailed)
            {
                MessageBox.Show(IPMan.App.Resources.Strings.ExistingInstanceNotResponding);
            }

            Shutdown();
            return;
        }

        ServiceCollection services = new();

        ConfigureServices(services, Dispatcher.CurrentDispatcher);

        _serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        MainWindow window = _serviceProvider.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();

        IUiDispatcher uiDispatcher = _serviceProvider.GetRequiredService<IUiDispatcher>();
        _activationHandler = new MainWindowActivationHandler(uiDispatcher, window);
        _activationChannelServer = _serviceProvider.GetRequiredService<IActivationChannelServer>();
        _activationChannelServer.ActivationRequested += _activationHandler.OnActivationRequested;
        _activationChannelServer.StartListening();

        _serviceProvider.GetRequiredService<MainWindowViewModel>().Initialize();

        _profileCatalog = _serviceProvider.GetRequiredService<IProfileCatalog>();
        _ = InitializeProfileCatalogAsync(_profileCatalog);
        _profileCatalog.StartWatching();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_activationChannelServer is not null)
        {
            if (_activationHandler is not null)
            {
                _activationChannelServer.ActivationRequested -=
                    _activationHandler.OnActivationRequested;
            }

            _activationChannelServer.StopListening();
        }

        _profileCatalog?.StopWatching();

        // Disposes the refresh coordinator and the network change monitor, which
        // releases the Windows network and profile directory subscriptions.
        _serviceProvider?.Dispose();

        _singleInstanceGuard?.Release();
        _singleInstanceGuard?.Dispose();

        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services, Dispatcher uiDispatcher)
    {
        // Logging sinks arrive in a later sprint; the null logger keeps the
        // infrastructure contracts satisfied without adding a logging provider.
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IDelayProvider, SystemDelayProvider>();
        services.AddSingleton<IElevationStateProvider, WindowsElevationStateProvider>();

        services.AddSingleton<IAdapterProbe, SystemNetworkInterfaceProbe>();
        services.AddSingleton<INetworkAdapterReader, NetworkAdapterReader>();
        services.AddSingleton(new Ipv4ConflictProbeOptions());
        services.AddSingleton<IIpv4ConflictProbe, PingIpv4ConflictProbe>();
        services.AddSingleton<IStaticIpv4ConfigurationValidator, StaticIpv4ConfigurationValidator>();
        services.AddSingleton<IStaticIpv4ConfigurationComparer, StaticIpv4ConfigurationComparer>();
        services.AddSingleton<INetworkConfigurationPreflightService, NetworkConfigurationPreflightService>();
        services.AddSingleton(new RecoverySnapshotRepositoryOptions
        {
            BackupDirectory = AppStorageLayout.BackupDirectory
        });
        services.AddSingleton(new ProfileRepositoryOptions
        {
            ProfilesDirectory = AppStorageLayout.ProfilesDirectory
        });
        services.AddSingleton(new AppSettingsRepositoryOptions
        {
            SettingsFilePath = AppStorageLayout.SettingsFilePath
        });
        services.AddSingleton(new ProfileWatcherOptions());
        services.AddSingleton<IRecoverySnapshotRepository, JsonRecoverySnapshotRepository>();
        services.AddSingleton<IProfileRepository, JsonProfileRepository>();
        services.AddSingleton<IAppSettingsRepository, JsonAppSettingsRepository>();
        services.AddSingleton<IProfileDirectoryWatcher, FileSystemProfileDirectoryWatcher>();
        services.AddSingleton<IProfileCatalog, ProfileCatalog>();
        services.AddSingleton<INetworkAdapterRecoveryReader, WmiNetworkAdapterRecoveryReader>();
        services.AddSingleton<NetworkMutationCoordinator>();
        services.AddSingleton<INetworkMutationCoordinator, CrossProcessNetworkMutationCoordinator>();
        services.AddSingleton<IIpv4DefaultRouteManager, WindowsIpv4DefaultRouteManager>();
        services.AddSingleton<INetworkAdapterConfigurator, WmiNetworkAdapterConfigurator>();
        services.AddSingleton(new StaticIpv4ApplyOptions());
        services.AddSingleton<IStaticIpv4ApplyService, StaticIpv4ApplyService>();
        services.AddSingleton<INetworkChangeMonitor, NetworkChangeMonitor>();
        services.AddSingleton(new AdapterRefreshCoordinatorOptions());
        services.AddSingleton<IAdapterRefreshCoordinator, AdapterRefreshCoordinator>();
        services.AddSingleton<IActivationChannelServer, NamedPipeActivationChannelServer>();

        services.AddSingleton<IUiDispatcher>(new WpfUiDispatcher(uiDispatcher));
        services.AddSingleton<IClipboardService, WpfClipboardService>();
        services.AddSingleton<IApplicationVersionProvider, AssemblyApplicationVersionProvider>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
    }

    private static void AllowExistingInstanceToSetForegroundWindow() =>
        _ = NativeMethods.AllowSetForegroundWindow(NativeMethods.AllowSetForegroundWindowAnyProcess);

    private static async Task InitializeProfileCatalogAsync(IProfileCatalog catalog)
    {
        try
        {
            await catalog.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Startup remains usable if a cancellation or unexpected subscriber
            // failure escapes the catalog's storage-failure isolation.
        }
    }

    private static class NativeMethods
    {
        public const uint AllowSetForegroundWindowAnyProcess = uint.MaxValue;

        [System.Runtime.InteropServices.DllImport(
            "user32.dll",
            ExactSpelling = true,
            SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool AllowSetForegroundWindow(uint processId);
    }
}

internal enum SingleInstanceStartupOutcome
{
    PrimaryInstance,
    ExistingInstanceActivated,
    ActivationFailed
}

/// <summary>
/// Keeps startup ownership and secondary-instance signalling testable without
/// constructing WPF application state or opening operating-system primitives.
/// </summary>
internal sealed class SingleInstanceStartupCoordinator
{
    private readonly ISingleInstanceGuard _guard;
    private readonly IActivationChannelClient _activationClient;
    private readonly Action _allowSetForegroundWindow;

    public SingleInstanceStartupCoordinator(
        ISingleInstanceGuard guard,
        IActivationChannelClient activationClient,
        Action allowSetForegroundWindow)
    {
        ArgumentNullException.ThrowIfNull(guard);
        ArgumentNullException.ThrowIfNull(activationClient);
        ArgumentNullException.ThrowIfNull(allowSetForegroundWindow);

        _guard = guard;
        _activationClient = activationClient;
        _allowSetForegroundWindow = allowSetForegroundWindow;
    }

    public async Task<SingleInstanceStartupOutcome> CoordinateAsync(
        CancellationToken cancellationToken)
    {
        SingleInstanceAcquisition acquisition = _guard.TryAcquire();

        if (acquisition != SingleInstanceAcquisition.Unavailable)
        {
            return SingleInstanceStartupOutcome.PrimaryInstance;
        }

        // The launching process transfers foreground permission immediately
        // before signalling, allowing the existing process to call Activate().
        _allowSetForegroundWindow();

        bool activationSent = await _activationClient
            .TrySendActivationAsync(cancellationToken)
            .ConfigureAwait(false);

        return activationSent
            ? SingleInstanceStartupOutcome.ExistingInstanceActivated
            : SingleInstanceStartupOutcome.ActivationFailed;
    }
}
