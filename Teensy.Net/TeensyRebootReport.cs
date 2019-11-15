namespace Teensy.Net
{

/// <summary>
/// This is a TeensyReport used for rebooting devices.
/// </summary>
internal class TeensyRebootReport : TeensyReport
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    public TeensyRebootReport() : base(3)
    {
        // https://www.pjrc.com/teensy/halfkay_protocol.html
        Initialize(0xFF);
    }
}

}
