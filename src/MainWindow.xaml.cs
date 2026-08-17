using System.Windows;

namespace RamAfk;

public partial class MainWindow : Window
{
    private bool _paused;
    public MainWindow() { InitializeComponent(); }
    private void Enable_Click(object sender, RoutedEventArgs e) => StatusText.Text = "Selected accounts enabled; the host activity clock will schedule staggered background Space posts.";
    private void Pause_Click(object sender, RoutedEventArgs e) { _paused = !_paused; StatusText.Text = _paused ? "All keep-alives paused." : "Keep-alives resumed."; }
}
