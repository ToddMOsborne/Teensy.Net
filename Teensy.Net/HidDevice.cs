namespace Teensy.Net
{

using System;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// A single Teensy that is running the bootloader. In this state, it is not
/// available as a serial device, but communication with the Teensy happens
/// using HID and the HalfKay protocol.
/// </summary>
internal class HidDevice : IDisposable
{
    /// <summary>
    /// Constructoror must be given path to device.
    /// </summary>
    public HidDevice(string path)
    {
        Path = path;

        // Get Vendor and Product IDs.
        if ( Open(false) )
        {
            var attributes =  default(HidNativeMethods.HIDD_ATTRIBUTES);
            attributes.Size = Marshal.SizeOf(attributes);
            
            if ( HidNativeMethods.HidD_GetAttributes(Handle, ref attributes) )
            {
                // Only interested in Teeny bootloaders.
                if ( attributes.ProductID == Constants.BootloaderId &&
                     attributes.VendorID  == Constants.VendorId )
                {
                    var capabilities = default(HidNativeMethods.HIDP_CAPS);
                    var pointer =      default(IntPtr);

                    if ( HidNativeMethods.HidD_GetPreparsedData(Handle,
                                                                ref pointer) )
                    {
                        if ( HidNativeMethods.HidP_GetCaps(
                            pointer,
                            ref capabilities) ==
                                HidNativeMethods.HIDP_STATUS_SUCCESS )
                        {
                            HidNativeMethods.HidD_FreePreparsedData(pointer);

                            if ( capabilities.OutputReportByteLength > 0 )
                            {
                                ReportLength = (ushort)
                                    capabilities.OutputReportByteLength;

                                switch ( capabilities.Usage )
                                {
                                    case 0x1B:
                                    {
                                        TeensyType = TeensyTypes.Teensy2;
                                        break;
                                    }

                                    case 0x1C:
                                    {
                                        TeensyType =
                                            TeensyTypes.Teensy2PlusPlus;

                                        break;
                                    }

                                    case 0x1D:
                                    {
                                        TeensyType = TeensyTypes.Teensy30;
                                        break;
                                    }

                                    case 0x1E:
                                    {
                                        TeensyType = TeensyTypes.Teensy31;
                                        break;
                                    }

                                    case 0x20:
                                    {
                                        TeensyType = TeensyTypes.TeensyLc;
                                        break;
                                    }

                                    case 0x21:
                                    {
                                        TeensyType = TeensyTypes.Teensy32;
                                        break;
                                    }
                                    
                                    case 0x1F:
                                    {
                                        TeensyType = TeensyTypes.Teensy35;
                                        break;
                                    }

                                    case 0x22:
                                    {
                                        TeensyType = TeensyTypes.Teensy36;
                                        break;
                                    }

                                    case 0x24:
                                    {
                                        TeensyType = TeensyTypes.Teensy40;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Close();
        }
    }

    /// <summary>
    /// Close device, as needed.
    /// </summary>
    private void Close()
    {
        if ( IsOpen )
        {
            HidNativeMethods.CloseHandle(Handle);

            Handle =          IntPtr.Zero;
            IsOpenReadWrite = false;
        }
    }

    /// <summary>
    /// Close device, as needed.
    /// </summary>
    public void Dispose() => Close();

    /// <summary>
    /// Given a serial number, find the Teensy device that is running the
    /// bootloader.
    /// </summary>
    internal static HidDevice FindDevice(uint serialNumber)
    {
        var list = HidNativeMethods.GetAllDevices(serialNumber);
        return list.Count > 0 ? list[0] : null;
    }

    /// <summary>
    /// The handle. The lifetime is managed by this object. Do not close it
    /// outside of the Close() method.
    /// </summary>
    public IntPtr Handle { get; private set; } = IntPtr.Zero;

    /// <summary>
    /// Is this device open?
    /// </summary>
    public bool IsOpen => Handle != IntPtr.Zero;

    /// <summary>
    /// If the device is open, this determines if it was opened with read and
    /// write access.
    /// </summary>
    private bool IsOpenReadWrite { get; set; }

    /// <summary>
    /// Open the device, as needed.
    /// </summary>
    public bool Open(bool readWrite = true)
    {
        // Open, but without read/write access?
        if ( IsOpen && readWrite && !IsOpenReadWrite )
        {
            Close();
        }

        if ( !IsOpen )
        {
            Handle = HidNativeMethods.CreateFile(
                Path,
                readWrite ? HidNativeMethods.GENERIC_READ |
                            HidNativeMethods.GENERIC_WRITE
                          : 0,
                HidNativeMethods.FILE_SHARE_READ |
                    HidNativeMethods.FILE_SHARE_WRITE,
                IntPtr.Zero,
                HidNativeMethods.OPEN_EXISTING,
                0,
                IntPtr.Zero);

            IsOpenReadWrite = readWrite;
        }

        return IsOpen;
    }

    /// <summary>
    /// Get the path to this device.
    /// </summary>
    private string Path { get; }

    /// <summary>
    /// The required output report length. This length include the first byte,
    /// which is the report ID.
    /// </summary>
    internal ushort ReportLength { get; }

    /// <summary>
    /// Get the serial number of this device.
    /// </summary>
    public uint SerialNumber
    {
        get
        {
            if ( _serialNumber == 0 && Open(false) )
            {
                var data = new byte[254];
                
                if ( HidNativeMethods.HidD_GetSerialNumberString(Handle,
                                                                 ref data[0],
                                                                 data.Length) )
                {
                    _serialNumber = Utility.FixSerialNumber(Convert.ToUInt32(
                        Encoding.Unicode.GetString(data).TrimEnd('\0'), 16));
                }
            }

            return _serialNumber;
        }
    }
    private uint _serialNumber;

    /// <summary>
    /// Get the type of Teensy device.
    /// </summary>
    public TeensyTypes TeensyType { get; } = TeensyTypes.Unknown;
}

}
