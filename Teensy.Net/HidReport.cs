namespace Teensy.Net
{

using System;
using System.Text;
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
        Data =   new byte[device.ReportLength];
        Device = device;
        Reset();
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
    private uint Index { get; set; } = 1;

    /// <summary>
    /// Reset so that buffer can be refilled. This zero fills it and sets the
    /// Data index to 1.
    /// </summary>
    protected void Reset()
    {
        for ( var i = 0; i < Data.Length; i++ )
        {
            Data[i] = 0;
        }

        Index = 1;
    }

    /// <summary>
    /// Set the current Data index to be at the DataOffset of the Teensy.
    /// </summary>
    protected bool SetDataStart(Teensy teensy)
    {
        var offset = 1 + teensy.DataOffset;
        var result = offset < Data.Length;

        if ( result )
        {
            Index = offset;
        }

        return result;
    }

    /// <summary>
    /// Write report data to the device.
    /// </summary>
    protected bool Write()
    {
        // If this fails, try again after a short delay.
        var result = WriteInternal(this is HidUploadReport);

        if ( !result )
        {
            Thread.Sleep(100);
            result = WriteInternal(this is HidUploadReport);
        }

        return result;
    }

    /// <summary>
    /// Write report data to the bootloader.
    /// </summary>
    private bool WriteInternal(bool test)
    {
        if ( Data.Length != Device.ReportLength )
        {
            throw new Exception("Invalid HID report length.");
        }

        var result = Device.Open();

        if ( result )
        {
            uint bytesWritten = 0;

            if ( test )
            {
                IntPtr h = HidNativeMethods.CreateFile("T:\\Scratch\\Output.TeensyNet",
                                                       0x0004,
                                                       0,
                                                       IntPtr.Zero,
                                                       4,
                                                       0,
                                                       IntPtr.Zero);

                result = HidNativeMethods.WriteFile(h, Data, Device.ReportLength, ref bytesWritten, IntPtr.Zero);
                HidNativeMethods.CloseHandle(h);

                h = HidNativeMethods.CreateFile("T:\\Scratch\\Output.TeensyNet.writes",
                                                       0x0004,
                                                       0,
                                                       IntPtr.Zero,
                                                       4,
                                                       0,
                                                       IntPtr.Zero);

                HidNativeMethods.WriteFile(h, Encoding.UTF8.GetBytes("X"), 1, ref bytesWritten, IntPtr.Zero);
                HidNativeMethods.CloseHandle(h);
            }
            else
            {
                result = HidNativeMethods.WriteFile(Device.Handle,
                                                    Data,
                                                    Device.ReportLength,
                                                    ref bytesWritten,
                                                    IntPtr.Zero) &&
                         bytesWritten == Device.ReportLength;
            }
        }

        return result;
    }
}

}
