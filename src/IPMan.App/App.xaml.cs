using System.IO;
using System.Windows;
using System.Windows.Threading;
using IPMan.App.Presentation;
using IPMan.App.ViewModels;
using IPMan.App.Views;
using IPMan.Application.Common;
using IPMan.Application.Networking;
using IPMan.Infrastructure.Common;
using IPMan.Infrastructure.Networking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IPMan.App;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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

        _serviceProvider.GetRequiredService<MainWindowViewModel>().Initialize();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Disposes the refresh coordinator and the network change monitor, which
        // releases the Windows network change subscriptions.
        _serviceProvider?.Dispose();
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
        services.AddSingleton(new RollbackSnapshotRepositoryOptions
        {
            BackupDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IPMan",
                "Backup")
        });
        services.AddSingleton<IRollbackSnapshotRepository, JsonRollbackSnapshotRepository>();
        services.AddSingleton<INetworkAdapterRecoveryReader, WmiNetworkAdapterRecoveryReader>();
        services.AddSingleton<INetworkMutationCoordinator, NetworkMutationCoordinator>();
        services.AddSingleton<IIpv4DefaultRouteManager, WindowsIpv4DefaultRouteManager>();
        services.AddSingleton<INetworkAdapterConfigurator, WmiNetworkAdapterConfigurator>();
        services.AddSingleton(new StaticIpv4ApplyOptions());
        services.AddSingleton<IStaticIpv4ApplyService, StaticIpv4ApplyService>();
        services.AddSingleton<INetworkChangeMonitor, NetworkChangeMonitor>();
        services.AddSingleton(new AdapterRefreshCoordinatorOptions());
        services.AddSingleton<IAdapterRefreshCoordinator, AdapterRefreshCoordinator>();

        services.AddSingleton<IUiDispatcher>(new WpfUiDispatcher(uiDispatcher));
        services.AddSingleton<IClipboardService, WpfClipboardService>();
        services.AddSingleton<IApplicationVersionProvider, AssemblyApplicationVersionProvider>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
    }
}
