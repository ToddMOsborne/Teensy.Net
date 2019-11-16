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
    public HidRebootReport(HidDevice device) : base(device)
    {
        // https://www.pjrc.com/teensy/halfkay_protocol.html
        SetByte(0, 0xFF);
        SetByte(1, 0xFF);
        SetByte(2, 0xFF);
    }
}

}
