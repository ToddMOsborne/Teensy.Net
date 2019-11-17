namespace Teensy.Net
{

using System;
using System.Threading;

/// <summary>
/// This is a HID report wrapper for communicating with HidDevices. It is used
/// to upload firmware to a device and reboot it. This is the common base
/// class used by HidReport and HidUploadReport.
/// </summary>
internal class HidReport
{
    /// <summary>
    /// Protected constructor must specify the HidDevice.
    /// </summary>
    protected HidReport(HidDevice device)
    {
        // We do not include the report ID in the Data array.
        Data =   new byte[device.ReportLength - 1];
        Device = device;
    }

    /// <summary>
    /// Add a byte of data, if able.
    /// </summary>
    protected bool AddData(byte b = 0xFF)
    {
        var result = Index < Data.Length;

        if ( result )
        {
            Data[Index] = b;
            ++Index;
        }

        return result;
    }

    /// <summary>
    /// Get the Data.
    /// </summary>
    private byte[] Data { get; }

    /// <summary>
    /// Get the device this report was created for.
    /// </summary>
    protected HidDevice Device { get; }

    /// <summary>
    /// Fill buffer will bytes.
    /// </summary>
    protected bool Fill(uint count = 1,
                        byte b =     0xFF)
    {
        var result = true;

        while ( result && count > 0 )
        {
            result = AddData(b);
            --count;
        }

        return result;
    }

    /// <summary>
    /// The current index where data will be added.
    /// </summary>
    private uint Index { get; set; }

    /// <summary>
    /// Set the current Data index to be at the DataOffset of the Teensy.
    /// </summary>
    protected bool SetDataStart(Teensy teensy)
    {
        var result = teensy.DataOffset < Data.Length;

        if ( result )
        {
            Index = teensy.DataOffset;
        }

        return result;
    }

    /// <summary>
    /// Write report data to the device. Following a successful write, the
    /// internal Data buffer will be reset to allow more writes.
    /// </summary>
    protected bool Write()
    {
        var data = new byte[Device.ReportLength];
        Data.CopyTo(data, 1);

        // If this fails, try again after a short delay.
        var result = Write(data);

        if ( !result )
        {
            Thread.Sleep(100);
            result = Write(data);
        }

        if ( result )
        {
            for ( var i = 0; i < Data.Length; i++ )
            {
                Data[i] = 0;
            }

            Index = 0;
        }

        return result;
    }

    /// <summary>
    /// Write report data to the bootloader.
    /// </summary>
    private bool Write(byte[] data)
    {
        if ( data.Length != Device.ReportLength )
        {
            throw new Exception("Invalid HID report length.");
        }

        bool WriteDevice()
        {
            var written = Device.Open();

            if ( written )
            {
                uint bytesWritten = 0;

                written = HidNativeMethods.WriteFile(Device.Handle,
                                                     data,
                                                     Device.ReportLength,
                                                     ref bytesWritten,
                                                     IntPtr.Zero) &&
                         bytesWritten == Device.ReportLength;
            }

            return written;
        }

        #if DEBUG
            var result = true;

            if ( this is HidUploadReport uploadReport &&
                 uploadReport.TestOutputStream  != null )
            {
                try
                {
                    uploadReport.TestOutputStream.Write(data,
                                                        0,
                                                        Device.ReportLength);
                }
                catch
                {
                    result = false;
                }
            }
            else
            {
                result = WriteDevice();
            }

            return result;
        #else
            return WriteDevice();
        #endif
    }
}

}
