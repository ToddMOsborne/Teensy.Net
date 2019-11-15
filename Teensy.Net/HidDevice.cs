namespace Teensy.Net
{

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// A single HID device.
/// </summary>
internal class HidDevice : IDisposable
{
    /// <summary>
    /// Constructor must be give name and path to device.
    /// </summary>
    public HidDevice(string name,
                     string path)
    {
        Name = name;
        Path = path;

        // Get Vendor and Product IDs.
        if ( Open(false) )
        {
            var attributes =  default(HidNative.HIDD_ATTRIBUTES);
            attributes.Size = Marshal.SizeOf(attributes);
            
            if ( HidNative.HidD_GetAttributes(Handle, ref attributes) )
            {
                ProductId = attributes.ProductID;
                VendorId =  attributes.VendorID;
            }

            var capabilities = default(HidNative.HIDP_CAPS);
            var pointer =      default(IntPtr);

            if ( HidNative.HidD_GetPreparsedData(Handle, ref pointer) )
            {
                HidNative.HidP_GetCaps(pointer, ref capabilities);
                HidNative.HidD_FreePreparsedData(pointer);

                BootloaderId = (ushort)capabilities.Usage;
            }

            Close();
        }
    }

    /// <summary>
    /// Get the Teensy bootloader ID.
    /// </summary>
    public ushort BootloaderId { get; private set; }

    /// <summary>
    /// Close device, as needed.
    /// </summary>
    private void Close()
    {
        if ( IsOpen )
        {
            if ( Environment.OSVersion.Version.Major > 5 )
            {
                HidNative.CancelIoEx(Handle, IntPtr.Zero);
            }
            
            HidNative.CloseHandle(Handle);

            Handle =          IntPtr.Zero;
            IsOpenReadWrite = false;
        }
    }

    /// <summary>
    /// Create a new report for this device.
    /// </summary>
    public HidReport CreateReport()
    {
        return new HidReport(1024);
    }

    /// <summary>
    /// Close device, as needed.
    /// </summary>
    public void Dispose() => Close();

    /// <summary>
    /// Get all Teensy devices. If looking for a specific one, pass the serial
    /// number.
    /// </summary>
    public static List<HidDevice> GetTeensies(uint serialNumber = 0)
    {
        var result = new List<HidDevice>();

        foreach ( var device in HidNative.GetAllDevices() )
        {
            if ( device.VendorId  == Constants.VendorId &&
                 device.ProductId == Constants.BootloaderId )
            {
                if ( serialNumber == 0 || serialNumber == device.SerialNumber )
                {
                    result.Add(device);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Handle.
    /// </summary>
    private IntPtr Handle { get; set; } = IntPtr.Zero;

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
    /// Get the name of this device.
    /// </summary>
    private string Name { get; }

    /// <summary>
    /// Open the device, as needed.
    /// </summary>
    private bool Open(bool readWrite = true)
    {
        // Open, but without read/write access?
        if ( IsOpen && readWrite && !IsOpenReadWrite )
        {
            Close();
        }

        if ( !IsOpen )
        {
            var security = new HidNative.SECURITY_ATTRIBUTES
            {
                lpSecurityDescriptor = IntPtr.Zero,
                bInheritHandle =       true
            };

            security.nLength = Marshal.SizeOf(security);

            Handle = HidNative.CreateFile(
                Path,
                readWrite ? HidNative.GENERIC_READ | HidNative.GENERIC_WRITE
                          : 0,
                HidNative.FILE_SHARE_READ | HidNative.FILE_SHARE_WRITE,
                ref security,
                HidNative.OPEN_EXISTING,
                0,
                0);

            IsOpenReadWrite = readWrite;
        }

        return IsOpen;
    }

    /// <summary>
    /// Get the path to this device.
    /// </summary>
    private string Path { get; }

    /// <summary>
    /// Get the Product ID.
    /// </summary>
    public ushort ProductId { get; private set; }

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
                
                if ( HidNative.HidD_GetSerialNumberString(Handle,
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
    /// Write report to device.
    /// </summary>
    public bool WriteReport(HidReport report)
    {
        return false;
    }

    /// <summary>
    /// Get the Vendor ID.
    /// </summary>
    public ushort VendorId { get; private set; }
}

}
