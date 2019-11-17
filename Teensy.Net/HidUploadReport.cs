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
                    "Cannot determine Teeny data block size.");
            }
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

            // Write the report if currently this method is working.
            bool WriteReport()
            {
                if ( result == UploadResults.Success )
                {
                    if ( !Write() )
                    {
                        result = UploadResults.ErrorUpload;
                    }
                }

                return result == UploadResults.Success;
            }

            // How much image do to write in each block.
            var writeLength = BlockSize == 512 || BlockSize == 1024
                              ? BlockSize + 64
                              : BlockSize + 2;

            writeLength = BlockSize;

            // Determine the total length of data that will be written.
            var totalLength = 0u;

            Image.Chunk(writeLength, (bytes, imageOffset) =>
            {
                totalLength = imageOffset + writeLength;
                return true;
            });

            Image.Chunk(writeLength, (bytes, imageOffset) =>
            {
                if ( imageOffset == 0 )
                {
                    Teensy.ProvideFeedback(
                        $"Erasing {Constants.TeensyWord} Flash Memory");
                }

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

                foreach ( var b in bytes )
                {
                    // If report data is full, time to write it.
                    if ( !AddData(b) )
                    {
                        if ( WriteReport() )
                        {
                            AddData(b);

                            // The first write erases the chip and needs a
                            // little longer to complete. Allow it 5 seconds.
                            if ( imageOffset == 0 )
                            {
                                #if DEBUG
                                    if ( TestOutputStream == null )
                                    {
                                        Thread.Sleep(5000);
                                    }
                                #else
                                    Thread.Sleep(5000);
                                #endif
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if ( result == UploadResults.Success )
                {
                    Teensy.ProvideFeedback(
                       Math.Min(imageOffset + writeLength, totalLength),
                       totalLength);
                }

                return result == UploadResults.Success;
            });

            // There could still be data buffered that needs to be written.
            WriteReport();

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
