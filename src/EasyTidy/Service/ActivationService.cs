using EasyTidy.Activation;
using EasyTidy.Common.Database;
using EasyTidy.Common.Extensions;
using EasyTidy.Contracts.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUIEx;

namespace EasyTidy.Service;

public class ActivationService : IActivationService
{
    private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler;
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
    private readonly IThemeSelectorService _themeSelectorService;
    private UIElement? _shell = null;
    private readonly AppDbContext _dbContext;

    public ActivationService(ActivationHandler<LaunchActivatedEventArgs> defaultHandler, IEnumerable<IActivationHandler> activationHandlers, IThemeSelectorService themeSelectorService)
    {
        _defaultHandler = defaultHandler;
        _activationHandlers = activationHandlers;
        _themeSelectorService = themeSelectorService;
        _dbContext = App.GetService<AppDbContext>();
    }

    public async Task ActivateAsync(object activationArgs)
    {
        // Execute tasks before activation.
        await InitializeAsync();

        // Set the MainWindow Content.
        if (App.MainWindow.Content == null)
        {
            _shell = App.GetService<ShellPage>();
            App.MainWindow.Content = _shell ?? new Frame();
        }

        // Handle activation via ActivationHandlers.
        await HandleActivationAsync(activationArgs);

        // Activate the MainWindow.
        SetWindowBehavior();
        // App.MainWindow.Activate();

        // Execute tasks after activation.
        await _dbContext.InitializeDatabaseAsync();
        await StartupAsync();
        await PerformStartupChecksAsync();
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        var activationHandler = _activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs));

        if (activationHandler != null)
        {
            await activationHandler.HandleAsync(activationArgs);
        }

        if (_defaultHandler.CanHandle(activationArgs))
        {
            await _defaultHandler.HandleAsync(activationArgs);
        }
    }

    private async Task InitializeAsync()
    {
        await _themeSelectorService.InitializeAsync().ConfigureAwait(false);
        await Task.CompletedTask;
    }

    private async Task StartupAsync()
    {
        // _themeSelectorService.ThemeChanged += (_, theme) => App.MainWindow.SetRequestedTheme(theme);
        await _themeSelectorService.SetRequestedThemeAsync();
        await Task.CompletedTask;
    }

    private async Task PerformStartupChecksAsync()
    {
        if ((bool)Settings.GeneralConfig.IsStartupCheck)
        {
            var app = App.GetService<AppUpdateSettingViewModel>();
            await app.CheckForNewVersionAsync();
        }

        await QuartzConfig.InitQuartzConfigAsync();
        await QuartzHelper.StartAllJob();
    }

    private void SetWindowBehavior()
    {
        App.MainWindow.Closed += (sender, args) =>
        {
            if (App.HandleClosedEvents)
            {
                args.Handled = true;
                App.MainWindow.Hide();
            }
        };

        if ((bool)Settings.GeneralConfig.Minimize)
        {
            // MainWindow.Activate();
            App.MainWindow.Hide();
        }
        else
        {
            App.MainWindow.Activate();
        }
    }
}
