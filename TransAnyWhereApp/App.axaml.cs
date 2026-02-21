using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TransAnyWhereApp.Services.Device;
using TransAnyWhereApp.Services.Network;
using TransAnyWhereApp.Services.QRCode;
using TransAnyWhereApp.Services.Storage;
using TransAnyWhereApp.ViewModels;
using TransAnyWhereApp.Views;

namespace TransAnyWhereApp;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void LoadLanguageResources()
    {
        try
        {
            var cultureName = System.Globalization.CultureInfo.CurrentUICulture.Name;
            bool isChinese = cultureName.StartsWith("zh", StringComparison.OrdinalIgnoreCase);

            var mergedDictionaries = Resources.MergedDictionaries;

            if (mergedDictionaries.Count >= 2)
            {
                if (isChinese)
                {
                    mergedDictionaries.RemoveAt(1);
                }
                else
                {
                    mergedDictionaries.RemoveAt(0);
                }
            }
        }
        catch
        {
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        LoadLanguageResources();

        var collection = new ServiceCollection();
        ConfigureServices(collection);
        Services = collection.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<ITransferService, TransferService>();
        services.AddSingleton<IQRCodeService, QRCodeService>();
        services.AddSingleton<IHtmlProvider, HtmlProvider>();
        services.AddSingleton<IDeviceManager, DeviceManager>();
        services.AddTransient<IFileReceiver, FileReceiver>();
        services.AddSingleton<Func<IFileReceiver>>(x => () => x.GetRequiredService<IFileReceiver>());

        services.AddTransient<MainViewModel>();
    }
}