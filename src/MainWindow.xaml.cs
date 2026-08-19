using System.Windows;
using System.Windows.Threading;

namespace RamAfk;

public partial class MainWindow : Window
{
    private readonly ManagedAccountRegistry _managedAccounts;
    private readonly DiagnosticsLog _diagnostics;
    private bool _paused;
    private int _loggedAccountCount = -1;

    public MainWindow(ManagedAccountRegistry? managedAccounts = null, DiagnosticsLog? diagnostics = null)
    {
        InitializeComponent();
        _managedAccounts = managedAccounts ?? new ManagedAccountRegistry();
        _diagnostics = diagnostics ?? new DiagnosticsLog();
        _managedAccounts.Changed += ManagedAccounts_Changed;
        _diagnostics.Added += Diagnostics_Added;
        var refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        refreshTimer.Tick += (_, _) => RefreshAccountList();
        refreshTimer.Start();
        RefreshAccountList();
    }
    private void Enable_Click(object sender, RoutedEventArgs e) => StatusText.Text = "Managed accounts enabled; AFK uses guarded foreground Space input and restores your prior client when safe.";
    private void Pause_Click(object sender, RoutedEventArgs e) { _paused = !_paused; StatusText.Text = _paused ? "All keep-alives paused." : "Keep-alives resumed; focus may switch briefly during delivery."; }

    private void ManagedAccounts_Changed(object? sender, int count)
    {
        if (count != _loggedAccountCount)
        {
            _loggedAccountCount = count;
            _diagnostics.Info($"Managed-account registry: {count} account(s).");
        }
        if (Dispatcher.CheckAccess()) RefreshAccountList();
        else
        {
            try { Dispatcher.BeginInvoke(new Action(RefreshAccountList)); }
            catch (InvalidOperationException) when (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) { }
            catch (TaskCanceledException) { }
        }
    }

    private void Diagnostics_Added(object? sender, DiagnosticEntry entry)
    {
        if (Dispatcher.CheckAccess()) AddDiagnosticToList(entry);
        else
        {
            try { Dispatcher.BeginInvoke(new Action(() => AddDiagnosticToList(entry))); }
            catch (InvalidOperationException) when (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) { }
            catch (TaskCanceledException) { }
        }
    }

    private void AddDiagnosticToList(DiagnosticEntry entry)
    {
        DiagnosticsList.Items.Add(entry.ToString());
        while (DiagnosticsList.Items.Count > 80) DiagnosticsList.Items.RemoveAt(0);
        DiagnosticsList.ScrollIntoView(entry.ToString());
    }

    private void RefreshAccountList()
    {
        var accounts = _managedAccounts.Snapshot();
        AccountList.ItemsSource = accounts.Count == 0
            ? new[] { "No managed accounts are running." }
            : accounts.Select(FormatAccount).ToArray();
    }

    private static string FormatAccount(ManagedAccountSnapshot account)
    {
        var idle = DateTime.UtcNow - account.LastActivityUtc;
        var state = idle <= TimeSpan.FromSeconds(90) ? "Active" : $"Idle {(int)idle.TotalHours:00}:{idle.Minutes:00}:{idle.Seconds:00}";
        return $"{account.Label}  —  {state}";
    }

    protected override void OnClosed(EventArgs e)
    {
        _diagnostics.Added -= Diagnostics_Added;
        _managedAccounts.Changed -= ManagedAccounts_Changed;
        base.OnClosed(e);
    }
}
