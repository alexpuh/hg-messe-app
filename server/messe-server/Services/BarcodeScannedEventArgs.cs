namespace Herrmann.MesseApp.Server.Services;

public class BarcodeScannedEventArgs(string barcode) : EventArgs
{
    public string Barcode { get; } = barcode;
    public bool IsProcessed { get; set; }
}