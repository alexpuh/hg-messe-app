using System.IO.Ports;

namespace Herrmann.MesseApp.Server.Services;

public class BarcodeScannerService(ILogger<BarcodeScannerService> logger) : IDisposable
{
    private SerialPort? serialPort;
    private bool isConnected;

    public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;
    public event EventHandler<ConnectionChangedEventArgs>? ConnectionChanged;

    public void Connect()
    {
        var ports = SerialPort.GetPortNames();
        var firstComPort = ports.FirstOrDefault();
        
        if (firstComPort == null)
        {
            SetConnectionState(false);
            throw new ApplicationException("Kein COM-Port gefunden");
        }

        logger.LogInformation("Verbinde mit COM-Port: {Port}", firstComPort);
        
        var sp = new SerialPort(firstComPort)
        {
            BaudRate = 9600,
            Parity = Parity.None,
            DataBits = 8,
            StopBits = StopBits.One,
            Handshake = Handshake.None,
            ReadTimeout = 500,
            WriteTimeout = 500
        };
        
        sp.Open();
        
        if (!sp.IsOpen)
        {
            sp.Dispose();
            SetConnectionState(false);
            throw new ApplicationException($"COM-Port {firstComPort} kann nicht geöffnet werden");
        }
        
        serialPort = sp;
        SetConnectionState(true);
        logger.LogInformation("Barcode-Scanner erfolgreich verbunden");
    }

    public bool IsConnected()
    {
        return serialPort is { IsOpen: true };
    }

    public void StartScan()
    {
        if (serialPort == null)
        {
            throw new InvalidOperationException("Scanner ist nicht verbunden");
        }

        logger.LogInformation("Starte Barcode-Scan-Prozess");
        
        while (IsConnected())
        {
            try
            {
                var line = serialPort.ReadLine();
                var barcode = string.Concat(line.Where(char.IsLetterOrDigit));
                
                if (string.IsNullOrWhiteSpace(barcode))
                {
                    continue;
                }

                logger.LogInformation("Barcode gescannt: {Barcode}", barcode);
                
                if (BarcodeScanned == null)
                {
                    SendError();
                }
                else
                {
                    var eventArgs = new BarcodeScannedEventArgs(barcode);
                    BarcodeScanned(this, eventArgs);
                    
                    if (eventArgs.IsProcessed)
                    {
                        logger.LogInformation("Scan: Success");
                        SendOk();
                    }
                    else
                    {
                        logger.LogError("Scan: Error");
                        SendError();
                    }
                }
            }
            catch (TimeoutException)
            {
                // Normal - keine Daten empfangen
            }
            catch (IOException ioException)
            {
                logger.LogWarning(ioException, "Scan-Prozess gestoppt - Verbindung verloren");
                SetConnectionState(false);
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fehler beim Scannen");
                SendError();
            }
        }
        
        logger.LogInformation("Scan-Prozess beendet");
    }

    private void SendError()
    {
        try
        {
            serialPort?.Write([0x07], 0, 1); // BEL (Fehler-Piep)
            logger.LogDebug("Fehler-Signal an Scanner gesendet");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Senden des Fehler-Signals");
        }
    }

    private void SendOk()
    {
        try
        {
            serialPort?.Write([0x06], 0, 1); // ACK (OK-Signal)
            logger.LogDebug("OK-Signal an Scanner gesendet");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Senden des OK-Signals");
        }
    }

    public void Disconnect()
    {
        if (serialPort != null)
        {
            serialPort.Close();
            serialPort.Dispose();
            serialPort = null;
            SetConnectionState(false);
            logger.LogInformation("Scanner getrennt");
        }
    }

    private void SetConnectionState(bool connected)
    {
        if (isConnected != connected)
        {
            isConnected = connected;
            logger.LogInformation("Verbindungsstatus geändert: {IsConnected}", connected);
            ConnectionChanged?.Invoke(this, new ConnectionChangedEventArgs(connected));
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}