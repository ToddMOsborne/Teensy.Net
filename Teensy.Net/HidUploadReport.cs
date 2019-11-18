namespace Teensy.Net
{

using System;
using System.IO;
using System.Threading;

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

        switch ( teensy.TeensyType )
        {
            case TeensyTypes.Teensy2:
            {
                BlockSize = 128;
                break;
            }

            case TeensyTypes.Teensy2PlusPlus:
            {
                BlockSize = 256;
                break;
            }

            case TeensyTypes.TeensyLc:
            {
                BlockSize = 512;
                break;
            }

            case TeensyTypes.Teensy30:
            case TeensyTypes.Teensy31:
            case TeensyTypes.Teensy32:
            case TeensyTypes.Teensy35:
            case TeensyTypes.Teensy36:
            case TeensyTypes.Teensy40:
            {
                BlockSize = 1024;
                break;
            }

            default:
            {
                throw new InvalidOperationException(
                    $"Cannot determine {Constants.TeensyWord} data block size.");
            }
        }

        // The block size must be less than the HID report length.
        if ( BlockSize > device.ReportLength - 1 )
        {
            throw new InvalidOperationException(
                $"The block size of a {Constants.TeensyWord} must be smaller than the HID report size, including the first byte, which is the report ID.");
        }
    }

    /// <summary>
    /// The size of each block we upload. This can be different from the HID
    /// report size.
    /// </summary>
    private uint BlockSize { get; }

    /// <summary>
    /// The HexImage.
    /// </summary>
    private HexImage Image { get; }

    /// <summary>
    /// The Teensy object.
    /// </summary>
    private Teensy Teensy { get; }

    /// <summary>
    /// Used for debugging only. This causes the upload report output normally
    /// sent to the Teensy to be directed to this file instead.
    /// </summary>
    #if DEBUG
        public FileStream TestOutputStream { get; private set; }
    #endif

    /// <summary>
    /// Upload an image to the Teensy.
    /// </summary>
    public UploadResults Upload()
    {
        var result = UploadResults.Success;

        // Bail?
        if ( Image.IsValid )
        {
            #if DEBUG
                const string filePath =
                    "T:\\Source\\Teensy.Net\\TestFiles\\blink.hex.TeensyNetOutput";

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if ( filePath != null )
                {
                    TestOutputStream = File.Open(filePath,
                                                 FileMode.Create,
                                                 FileAccess.Write,
                                                 FileShare.None);
                }
            #endif

            // Determine the total length of data that will be written.
            var totalLength = 0u;

            Image.Chunk(BlockSize, (bytes, imageOffset) =>
            {
                totalLength = imageOffset + BlockSize;
                return true;
            });

            Image.Chunk(BlockSize, (bytes, imageOffset) =>
            {
                // The data offset is how much free space to leave in the HID
                // report data before writing of actual image data. The address
                // is always first, but writing image data should occur at this
                // offset.
                var dataOffset = 2u;

                // Add address (image offset).
                if ( BlockSize <= 256 && Teensy.FlashSize < 0x10000 )
                {
                    AddData((byte)(imageOffset & 0xFF));
                    AddData((byte)((imageOffset >> 8) & 0xFF));
                }
                else if ( BlockSize == 256 )
                {
                    AddData((byte)((imageOffset >> 8)  & 0xFF));
                    AddData((byte)((imageOffset >> 16) & 0xFF));
                }
                else
                {
                    AddData((byte)(imageOffset & 0xFF));
                    AddData((byte)((imageOffset >> 8)  & 0xFF));
                    AddData((byte)((imageOffset >> 16) & 0xFF));

                    dataOffset = 64;
                }

                // Copy data to report, starting at data offset.
                SetDataStart(dataOffset);
                Fill(bytes);

                if ( imageOffset == 0 )
                {
                    Teensy.ProvideFeedback(
                        $"Erasing {Constants.TeensyWord} Flash Memory");
                }

                if ( Write() )
                {
                    const int sleep = 3 * 1000;

                    // The first write erases the chip and needs a
                    // little longer to complete.
                    #if DEBUG
                        if ( TestOutputStream == null )
                        {
                            Thread.Sleep(sleep);
                        }
                    #else
                        Thread.Sleep(sleep);
                    #endif

                    Teensy.ProvideFeedback(imageOffset, totalLength);
                }
                else
                {
                    result = UploadResults.ErrorUpload;
                }

                return result == UploadResults.Success;
            });

            #if DEBUG
                if ( TestOutputStream != null )
                {
                    TestOutputStream.Close();
                    TestOutputStream.Dispose();
                    TestOutputStream = null;
                }
            #endif

            // One final callback for 100%?
            Teensy.ProvideFeedback(totalLength, totalLength);
        }
        else
        {
            result = UploadResults.ErrorInvalidHexImage;
        }

        return result;
    }
}

}
