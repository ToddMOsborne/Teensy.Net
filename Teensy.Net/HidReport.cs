namespace Teensy.Net
{

/// <summary>
/// This is a HID report wrapper for communicating with HidDevices. It is used
/// to upload firmware to a device and reboot it. This is the common base
/// class used by HidReport and HidUploadReport.
/// </summary>
internal class HidReport
{
    /// <summary>
    /// Protected constructor must specify the data length.
    /// </summary>
    protected HidReport(uint dataLength)
    {
        Data = new byte[dataLength];
        Initialize();
    }

    /// <summary>
    /// Get the data.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Initialize a HID report by setting all Data to 0, or specified
    /// character.
    /// </summary>
    public void Initialize(byte b = 0)
    {
        for ( var i = 0; i < Data.Length; i++ )
        {
            Data[i] = b;
        }
    }
}

}
