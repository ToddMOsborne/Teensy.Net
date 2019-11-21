namespace TeensyNet
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
        if ( device.ReportLength == 0 )
        {
            throw new TeensyException(
                "The HID report length must be known.");
        }

        // We include the report ID in the Data array.
        Data =   new byte[device.ReportLength];
        Device = device;
    }

    /// <summary>
    /// Add a byte of data.
    /// </summary>
    protected void AddData(byte b = 0xFF)
    {
        if ( Index == Data.Length )
        {
            throw new TeensyException(
                "Cannot add any more data to HID report.");
        }

        Data[Index] = b;
        ++Index;

        IsDirty = true;
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
    protected void Fill(uint count = 1,
                        byte b =     0xFF)
    {
        while ( count > 0 )
        {
            AddData(b);
            --count;
        }
    }

    /// <summary>
    /// Fill from a byte array.
    /// </summary>
    protected void Fill(byte[] bytes)
    {
        foreach ( var b in bytes )
        {
            AddData(b);
        }
    }

    /// <summary>
    /// The current index where data will be added.
    /// </summary>
    private uint Index { get; set; } = 1;

    /// <summary>
    /// Determine if there is buffered data that needs to be written.
    /// </summary>
    private bool IsDirty { get; set; }

    /// <summary>
    /// Set the current Data index.
    /// </summary>
    protected void SetDataStart(uint index)
    {
        if ( index + 1 >= Data.Length )
        {
            throw new TeensyException(
                "Data string index is outside of the HID report length.");
        }

        Index = index + 1;
    }

    /// <summary>
    /// Write report data to the device. Following a successful write, the
    /// internal Data buffer will be reset to allow more writes.
    /// </summary>
    protected void Write()
    {
        // If nothing to write, say everything is fine.
        if ( !IsDirty )
        {
            return;
        }

        // If this fails, try again after a short delay.
        var result = WriteInternal();

        if ( !result )
        {
            Thread.Sleep(100);
            result = WriteInternal();
        }

        if ( !result )
        {
            throw new TeensyException("Failed writing HID record.");
        }

        for ( var i = 1; i < Data.Length; i++ )
        {
            Data[i] = 0;
        }

        Index =   1;
        IsDirty = false;
    }

    /// <summary>
    /// Write report data to the bootloader.
    /// </summary>
    private bool WriteInternal()
    {
        bool WriteDevice()
        {
            var written = Device.Open();

            if ( written )
            {
                uint bytesWritten = 0;

                written = HidNativeMethods.WriteFile(Device.Handle,
                                                     Data,
                                                     Device.ReportLength,
                                                     ref bytesWritten,
                                                     IntPtr.Zero) &&
                         bytesWritten == Device.ReportLength;
            }

            if ( written )
            {
                Thread.Sleep(10);
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
                    uploadReport.TestOutputStream.Write(Data,
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
