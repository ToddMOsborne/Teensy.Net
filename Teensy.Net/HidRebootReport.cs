namespace Teensy.Net
{

/// <summary>
/// This is a HID report used for rebooting devices. Simply creating one of
/// these objects 
/// </summary>
internal class HidRebootReport : HidReport
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    public HidRebootReport(HidDevice device) : base(device)
    {
        // https://www.pjrc.com/teensy/halfkay_protocol.html
        Fill(3);
    }

    /// <summary>
    /// Reboot now.
    /// </summary>
    public void Reboot() => Write();
}

}
