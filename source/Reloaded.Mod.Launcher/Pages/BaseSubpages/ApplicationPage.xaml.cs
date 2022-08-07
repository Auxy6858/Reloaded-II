﻿using ApplicationSubPage = Reloaded.Mod.Launcher.Lib.Models.Model.Pages.ApplicationSubPage;
using Environment = Reloaded.Mod.Shared.Environment;
using WindowViewModel = Reloaded.Mod.Launcher.Lib.Models.ViewModel.WindowViewModel;

namespace Reloaded.Mod.Launcher.Pages.BaseSubpages;

/// <summary>
/// Interaction logic for ApplicationPage.xaml
/// </summary>
public partial class ApplicationPage : ReloadedIIPage, IDisposable
{
    public ApplicationViewModel ViewModel { get; set; }

    public ApplicationPage()
    {
        this.AnimateOutStarted += Dispose;
        InitializeComponent();
        ViewModel = new ApplicationViewModel(Lib.IoC.Get<MainPageViewModel>().SelectedApplication!, Lib.IoC.Get<ModConfigService>(), Lib.IoC.Get<ModUserConfigService>(), Lib.IoC.Get<LoaderConfig>());
        ViewModel.PropertyChanged += OnPageChanged;
        ViewModel.ChangeApplicationPage(ApplicationSubPage.ApplicationSummary);
    }

    private void OnPageChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.Page))
            SwitchPage(ViewModel.Page);
    }

    ~ApplicationPage()
    {
        Dispose();
    }

    public void Dispose()
    {
        this.AnimateOutStarted -= Dispose;
        ActionWrappers.ExecuteWithApplicationDispatcherAsync(() => PageHost.CurrentPage?.AnimateOut());
        ViewModel?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void ReloadedMod_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: Process process }) 
            return;

        ViewModel.SelectedProcess = process;
        ViewModel.ChangeApplicationPage(ApplicationSubPage.ReloadedProcess);
    }

    private void NonReloadedMod_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: Process process }) 
            return;

        ViewModel.SelectedProcess = process;
        if (!process.HasExited)
            ViewModel.ChangeApplicationPage(ApplicationSubPage.NonReloadedProcess);
    }

    private void Summary_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel.ChangeApplicationPage(ApplicationSubPage.ApplicationSummary);
    }

    private void Edit_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel.ChangeApplicationPage(ApplicationSubPage.EditApplication);
    }

    private async void LaunchApplication_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        await ViewModel.ApplicationTuple.SaveAsync();
        ViewModel.EnforceModCompatibility();
        await Setup.CheckForMissingModDependenciesAsync();

        var appTuple = ViewModel.ApplicationTuple;
        var launcher  = ApplicationLauncher.FromApplicationConfig(appTuple);

        if (!Environment.IsWine || (Environment.IsWine && CompatibilityDialogs.WineShowLaunchDialog()))
            launcher.Start();
    }

    private void MakeShortcut_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.MakeShortcutCommand.CanExecute(null))
            ViewModel.MakeShortcutCommand.Execute(null);
    }

    private void LoadModSet_PreviewMouseDown(object sender, MouseButtonEventArgs e) => ViewModel.LoadModSet();
    private void SaveModSet_PreviewMouseDown(object sender, MouseButtonEventArgs e) => ViewModel.SaveModSet();

    /* Animation/Title/Setup overrides */
    protected override Animation[] MakeExitAnimations()
    {
        return new Animation[]
        {
            new RenderTransformAnimation(-this.ActualWidth, RenderTransformDirection.Horizontal, RenderTransformTarget.Away, null, XamlExitSlideAnimationDuration.Get()),
            new OpacityAnimation(XamlExitFadeAnimationDuration.Get(), 1, XamlExitFadeOpacityEnd.Get())
        };
    }

    protected override void OnAnimateInFinished()
    {
        if (!String.IsNullOrEmpty(this.Title))
            Lib.IoC.Get<WindowViewModel>().WindowTitle = $"{this.Title}: {ViewModel.ApplicationTuple.Config.AppName}";
    }

    // Switch to new page.
    private void SwitchPage(ApplicationSubPage page)
    {
        PageHost.CurrentPage = page switch
        {
            ApplicationSubPage.Null => null,
            ApplicationSubPage.NonReloadedProcess => new NonReloadedProcessPage(ViewModel),
            ApplicationSubPage.ReloadedProcess => new ReloadedProcessPage(ViewModel),
            ApplicationSubPage.ApplicationSummary => new AppSummaryPage(ViewModel),
            ApplicationSubPage.EditApplication => new EditAppPage(ViewModel),
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };
    }
}