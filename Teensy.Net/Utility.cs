namespace Teensy.Net
{

/// <summary>
/// Static helper methods.
/// </summary>
public static class Utility
{
    /// <summary>
    /// Given a serial number, find the HID device.
    /// </summary>
    internal static HidDevice FindHidDevice(uint serialNumber)
    {
        var list = HidDevice.GetTeensies(serialNumber);
        return list.Count > 0 ? list[0] : null;
    }

    /// <summary>
    /// If serialNumber is not invalid, multiply be 10.
    /// </summary>
    public static uint FixSerialNumber(uint serialNumber)
    {
        if ( serialNumber != 0xFFFFFFFF )
        {
            serialNumber *= 10;
        }

        return serialNumber;
    }
}

}