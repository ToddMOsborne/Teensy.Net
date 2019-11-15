namespace Teensy.Net
{

/// <summary>
/// This is a HID report used for rebooting devices.
/// </summary>
internal class HidRebootReport : HidReport
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    public HidRebootReport() : base(3)
    {
        // https://www.pjrc.com/teensy/halfkay_protocol.html
        Initialize(0xFF);
    }
}

}
