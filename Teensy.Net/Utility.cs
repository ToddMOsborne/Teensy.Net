namespace Teensy.Net
{

/// <summary>
/// Static helper methods.
/// </summary>
internal static class Utility
{
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