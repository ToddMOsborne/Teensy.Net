namespace Teensy.Net
{

using System;

/// <summary>
/// This is a HID report used for uploading firmware.
/// </summary>
internal class HidUploadReport : HidReport
{
    /// <summary>
    /// Constructor must be initialized with the HidDevice, the Teensy this
    /// report is for and the HexImage object to upload.
    /// </summary>
    public HidUploadReport(HidDevice device,
                           Teensy    teensy,
                           HexImage  image) : base(device)
    {
        Teensy = teensy;
        Image =  image;
    }

    /// <summary>
    /// The HexImage.
    /// </summary>
    private HexImage Image { get; }

    /// <summary>
    /// Set the report data for uploading part of an image. On input,
    /// imageOffset specifies the starting point in the image. On exit, it
    /// will contain the offset for the next iteration. This method returns
    /// true to indicate there is indeed another iteration needed, false when
    /// complete.
    /// </summary>
    public bool InitializeImageBlock(ref uint imageOffset,
                                     out bool shouldUpload)
    {
        shouldUpload = true;

        // Determine if the image at the specified block is empty.
        var empty = imageOffset != 0;

        if ( empty )
        {
            var testImageOffset = imageOffset;
            
            var imageEnd = Math.Min(testImageOffset + Device.ReportLength,
                                    (uint)Image.Data.Length);

            while ( testImageOffset < imageEnd )
            {
                if ( Image.Data[testImageOffset] != 0xFF )
                {
                    empty = false;
                    break;
                }

                ++testImageOffset;
            }
        }

        // Empty?
        if ( empty )
        {
            imageOffset += Device.ReportLength;
            shouldUpload = false;
        }
        else
        {
            // Copy address bytes to report.
            var address = BitConverter.GetBytes((int)imageOffset);

            // There are (mostly) common.
            SetByte(0, address[0]);
            SetByte(1, address[1]);

            switch ( Teensy.TeensyType )
            {
                case TeensyTypes.Teensy2PlusPlus:
                {
                    SetByte(0, address[1]);
                    SetByte(1, address[2]);
                    break;
                }

                case TeensyTypes.TeensyLc:
                case TeensyTypes.Teensy30:
                case TeensyTypes.Teensy31:
                case TeensyTypes.Teensy32:
                case TeensyTypes.Teensy35:
                case TeensyTypes.Teensy36:
                case TeensyTypes.Teensy40:
                {
                    SetByte(2, address[2]);
                    break;
                }
            }
            
            // Copy data to report.
            var reportOffset = Teensy.DataOffset + 1;
            
            while ( reportOffset < Length )
            {
                if ( imageOffset < Image.Data.Length)
                {
                    SetByte(reportOffset, Image.Data[imageOffset]);
                    ++imageOffset;
                }
                else
                {
                    ClearByte(reportOffset);
                }

                ++reportOffset;
            }
        }

        return imageOffset < Image.Data.Length;
    }

    /// <summary>
    /// The Teensy object.
    /// </summary>
    private Teensy Teensy { get; }
}

}
