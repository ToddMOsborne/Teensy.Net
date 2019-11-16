namespace Teensy.Net
{

/// <summary>
/// This is a HID report wrapper for communicating with HidDevices. It is used
/// to upload firmware to a device and reboot it. This is the common base
/// class used by HidReport and HidUploadReport.
/// </summary>
internal class HidReport
{
    private readonly byte[] _bytes;

    /// <summary>
    /// Protected constructor must specify the data length.
    /// </summary>
    protected HidReport(HidDevice device)
    {
        Device = device;
        _bytes = new byte[device.ReportLength];
    }

    /// <summary>
    /// Set the byte at the specified index to 0.
    /// </summary>
    protected void ClearByte(uint index) => SetByte(index, 0);

    /// <summary>
    /// Get the device this report was created for.
    /// </summary>
    protected HidDevice Device { get; }

    /// <summary>
    /// Get the raw bytes.
    /// </summary>
    public byte[] GetBytes() => _bytes;

    /// <summary>
    /// Get the length of the data block.
    /// </summary>
    public uint Length => (uint)_bytes.Length;

    /// <summary>
    /// Set the byte at the specified index.
    /// </summary>
    protected void SetByte(uint index,
                           byte value) => _bytes[index] = value;
}

}
