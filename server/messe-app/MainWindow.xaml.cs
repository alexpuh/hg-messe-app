using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;

namespace Herrmann.MesseApp.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private static readonly string BasePath = Path.GetDirectoryName(AppDomain.CurrentDomain.SetupInformation.ApplicationBase)!;
    
    private Process? serverProcess;
    private const string ServerUrl = "http://localhost:5227";
    private readonly HttpClient httpClient = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void MainWindow_Loaded(object sender, RoutedEventArgs eventArgs)
    {
        // Initialisiere WebView2
        await WebView.EnsureCoreWebView2Async();

        // Starte den Server
        await StartServerAsync();
    }

    private async Task StartServerAsync()
    {
        try
        {
            // Finde den Pfad zur Server-Anwendung
            var serverPath = FindServerExecutable();
            
            if (string.IsNullOrEmpty(serverPath))
            {
                UpdateStatus("Server nicht gefunden!", false);
                MessageBox.Show(
                    "Die Server-Anwendung (messe-server.exe) wurde nicht gefunden.\n\n" +
                    "Bitte stellen Sie sicher, dass die Anwendung kompiliert wurde.",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            UpdateStatus("Server wird gestartet...", false);

            // Starte den Server-Prozess
            serverProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = serverPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(serverPath)
                }
            };
            serverProcess.Start();

            // Warte, bis der Server bereit ist
            var serverReady = await WaitForServerAsync();

            if (serverReady)
            {
                UpdateStatus("Server läuft", true);
                
                // Lade die UI
                WebView.Source = new Uri(ServerUrl);
            }
            else
            {
                UpdateStatus("Server konnte nicht gestartet werden", false);
                MessageBox.Show(
                    "Der Server konnte nicht gestartet werden oder ist nicht erreichbar.",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            UpdateStatus("Fehler beim Starten des Servers", false);
            MessageBox.Show(
                $"Fehler beim Starten des Servers:\n{ex.Message}",
                "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string? FindServerExecutable()
    {
        // Mögliche Pfade zur Server-Anwendung
        var possiblePaths = new[]
        {
            Path.Combine(BasePath, "server", "messe-server.exe"),
        };
        return possiblePaths.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
    }

    private async Task<bool> WaitForServerAsync()
    {
        // Warte bis zu 30 Sekunden, bis der Server bereit ist
        await Task.Delay(500);
        for (var i = 0; i < 60; i++)
        {
            try
            {
                var response = await httpClient.GetAsync(ServerUrl);
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Server antwortet (404 ist OK, bedeutet nur, dass die Root-Route nicht existiert)
                    return true;
                }
            }
            catch
            {
                // Server noch nicht bereit
            }

            await Task.Delay(500);
        }

        return false;
    }

    private void UpdateStatus(string message, bool isRunning)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = message;
            StatusIndicator.Fill = isRunning 
                ? new SolidColorBrush(Colors.Green) 
                : new SolidColorBrush(Colors.Orange);
        });
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Beende den Server-Prozess beim Schließen der Anwendung
        if (serverProcess is { HasExited: false })
        {
            try
            {
                serverProcess.Kill(true); // true = töte auch Child-Prozesse
                serverProcess.WaitForExit(5000);
                serverProcess.Dispose();
            }
            catch
            {
                // Ignoriere Fehler beim Beenden
            }
        }

        httpClient.Dispose();
    }
}