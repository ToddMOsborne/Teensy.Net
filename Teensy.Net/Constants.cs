namespace Teensy.Net
{

/// <summary>
/// Constants and pseudo-constants.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Vendor ID for Teensy devices.
    /// </summary>
    public static uint BootloaderId { get; set; } = 0x478;

    /// <summary>
    /// Magic baud rate to start bootloader..
    /// </summary>
    public static uint MagicBaudRate { get; set; } = 134;

    /// <summary>
    /// This string defines what a Teensy is called when generating exceptions
    /// or providing feedback. The default is "Teensy".
    /// </summary>
    public static string TeensyWord { get; set; } = "Teensy";

    /// <summary>
    /// Vendor ID for Teensy devices.
    /// </summary>
    public static uint VendorId { get; set; } = 0x16C0;
}

/// <summary>
/// Suppported Teensy types.
/// </summary>
public enum TeensyTypes
{
    /// <summary>
    /// Unknown, invalid, etc.
    /// </summary>
    Unknown,
    /// <summary>
    /// Teensy 4.
    /// </summary>
    Teensy40,
    /// <summary>
    /// Teensy 3.6.
    /// </summary>
    Teensy36,
    /// <summary>
    /// Teensy 3.5.
    /// </summary>
    Teensy35,
    /// <summary>
    /// Teensy 3.2.
    /// </summary>
    Teensy32,
    /// <summary>
    /// Teensy 3.1.
    /// </summary>
    Teensy31,
    /// <summary>
    /// Teensy 3.0.
    /// </summary>
    Teensy30,
    /// <summary>
    /// Teensy LC.
    /// </summary>
    TeensyLc,
    /// <summary>
    /// Teensy 2++.
    /// </summary>
    Teensy2PlusPlus,
    /// <summary>
    /// Teensy 2.
    /// </summary>
    Teensy2
}

/// <summary>
/// Types of USB devices.
/// </summary>
public enum UsbTypes
{
    /// <summary>
    /// Disconnected.
    /// </summary>
    Disconnected,
    /// <summary>
    /// The bootloader is running.
    /// </summary>
    Bootloader,
    /// <summary>
    /// Serial.
    /// </summary>
    Serial
}


}
