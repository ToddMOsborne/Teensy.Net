namespace Teensy.Net
{

/// <summary>
/// HID report.
/// </summary>
internal class HidReport
{
    public HidReport(uint size)
    {
        Data = new byte[size];
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
            Data[0] = b;
        }
    }
}

}
