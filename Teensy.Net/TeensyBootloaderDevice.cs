namespace Teensy.Net
{

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

/// <summary>
/// A single Teensy that is running the bootloader. In this state, it is not
/// available as a serial device, but communication with the Teensy happens
/// using HID and the HalfKay protocol.
/// </summary>
internal class TeensyBootloaderDevice : IDisposable
{
    /// <summary>
    /// Constructoror must be given path to device.
    /// </summary>
    public TeensyBootloaderDevice(string path)
    {
        Path = path;

        // Get Vendor and Product IDs.
        if ( Open(false) )
        {
            var attributes =  default(NativeMethods.HIDD_ATTRIBUTES);
            attributes.Size = Marshal.SizeOf(attributes);
            
            if ( NativeMethods.HidD_GetAttributes(Handle, ref attributes) )
            {
                ProductId = attributes.ProductID;
                VendorId =  attributes.VendorID;
            }

            var capabilities = default(NativeMethods.HIDP_CAPS);
            var pointer =      default(IntPtr);

            if ( NativeMethods.HidD_GetPreparsedData(Handle, ref pointer) )
            {
                NativeMethods.HidP_GetCaps(pointer, ref capabilities);
                NativeMethods.HidD_FreePreparsedData(pointer);

                switch ( capabilities.Usage )
                {
                    case 0x1B:
                    {
                        TeensyType = TeensyTypes.Teensy2;
                        break;
                    }

                    case 0x1C:
                    {
                        TeensyType = TeensyTypes.Teensy2PlusPlus;
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
            NativeMethods.CloseHandle(Handle);

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
    internal static TeensyBootloaderDevice FindDevice(uint serialNumber)
    {
        var list = NativeMethods.GetAllDevices(serialNumber);
        return list.Count > 0 ? list[0] : null;
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
            var security = new NativeMethods.SECURITY_ATTRIBUTES
            {
                lpSecurityDescriptor = IntPtr.Zero,
                bInheritHandle =       true
            };

            security.nLength = Marshal.SizeOf(security);

            Handle = NativeMethods.CreateFile(
                Path,
                readWrite ? NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE
                          : 0,
                NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
                ref security,
                NativeMethods.OPEN_EXISTING,
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
                
                if ( NativeMethods.HidD_GetSerialNumberString(Handle,
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

    /// <summary>
    /// Upload an image to the Teensy.
    /// </summary>
    public UploadResults Upload(Teensy   teensy,
                                HexImage image)
    {
        // Bail?
        if ( !image.IsValid )
        {
            return UploadResults.ErrorInvalidHexImage;
        }

        var result = UploadResults.Success;
        var data =   image.Data;
        var length = (uint)data.Length;

        bool IsEmptyBlock(uint offset)
        {
            var empty = true;
            var end =   offset + teensy.BlockSize;

            while ( offset < end )
            {
                if ( data[offset] != 0xFF )
                {
                    empty = false;
                    break;
                }

                ++offset;
            }

            return empty;
        }

        var report = new TeensyUploadReport(teensy, image);

        for ( uint offset = 0; offset < length; offset += teensy.BlockSize )
        {
            // If the block is empty, skip it. This does not apply to the first
            // block though.
            if ( offset == 0 || !IsEmptyBlock(offset) )
            {
                if ( offset == 0 )
                {
                    teensy.ProvideFeedback(
                        $"Erasing {Constants.TeensyWord} Flash Memory");
                }

                report.InitializeImageBlock(offset);

                if ( Write(report) )
                {
                    // The first write erases the chip and needs a little
                    // longer to complete. Allow it 5 seconds. After that, use
                    // 1/2 second. This is taken from the code for teensy
                    // loader at:
                    // https://github.com/PaulStoffregen/teensy_loader_cli/blob/master/teensy_loader_cli.c
                    Thread.Sleep(offset == 0 ? 5000 : 500);
                }
                else
                {
                    result = UploadResults.ErrorUpload;
                    break;
                }
            }

            teensy.ProvideFeedback(offset, length);
        }

        // One final callback for 100%?
        teensy.ProvideFeedback(length, length);

        return result;
    }

    /// <summary>
    /// Get the Vendor ID.
    /// </summary>
    public ushort VendorId { get; }

    /// <summary>
    /// Write report data to the bootloader.
    /// </summary>
    public bool Write(TeensyReport report)
    {
        // If this fails, try again after a short delay.
        var result = WriteInternal(report);

        if ( !result )
        {
            Thread.Sleep(100);
            result = WriteInternal(report);
        }

        return result;
    }

    /// <summary>
    /// Write report data to the bootloader.
    /// </summary>
    private bool WriteInternal(TeensyReport report)
    {
        var result = Open();

        if ( result )
        {
            var overlapped = new NativeOverlapped();
            
            result = NativeMethods.WriteFile(Handle,
                                             report.Data,
                                             (uint)report.Data.Length,
                                             out var bytesWritten,
                                             ref overlapped) &&
                     bytesWritten == report.Data.Length;
        }

        return result;
    }
}

}
