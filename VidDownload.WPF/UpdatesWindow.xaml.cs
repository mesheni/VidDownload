using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui;
using VidDownload.WPF.Services;

namespace VidDownload.WPF;

public partial class UpdatesWindow : Wpf.Ui.Controls.FluentWindow
{
    public UpdatesWindow(object viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        if (Application.Current.MainWindow is MainWindow mainOwner)
            Owner = mainOwner;
        var dialogs = AppServices.ServiceProvider.GetRequiredService<IContentDialogService>();
        Loaded += (_, _) => dialogs.SetDialogHost(DialogHost);
        Closed += (_, _) =>
        {
            if (Owner is MainWindow main)
                dialogs.SetDialogHost(main.DialogHost);
        };
    }
}
