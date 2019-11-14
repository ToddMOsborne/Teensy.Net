namespace Teensy.Net
{

using System;
using System.Text;
using HidLibrary;

/// <summary>
/// Static helper methods.
/// </summary>
public static class Utility
{
    /// <summary>
    /// Given a serial number, find the HID device.
    /// </summary>
    public static HidDevice FindHidDevice(uint serialNumber)
    {
        HidDevice result = null;

        foreach ( var device in HidDevices.Enumerate(
             (int)Constants.VendorId,
             (int)Constants.BootloaderId) )
        {
            device.ReadSerialNumber(out var snBytes);

            var testSerialNumber = FixSerialNumber(Convert.ToUInt32(
                Encoding.Unicode.GetString(snBytes).TrimEnd('\0'),
                16));

            if ( testSerialNumber == serialNumber )
            {
                result = device;
                break;
            }
        }

        return result;
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