namespace Teensy.Net
{

using System;
using System.IO;
using System.Runtime.CompilerServices;
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
    }

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

            uint imageOffset = 0;

            while ( WriteBlock(ref imageOffset, ref result) ) {}

            #if DEBUG
                if ( TestOutputStream != null )
                {
                    TestOutputStream.Close();
                    TestOutputStream.Dispose();
                    TestOutputStream = null;
                }
            #endif

            // One final callback for 100%?
            Teensy.ProvideFeedback((uint)Image.Data.Length,
                                   (uint)Image.Data.Length);
        }
        else
        {
            result = UploadResults.ErrorInvalidHexImage;
        }

        return result;
    }

    /// <summary>
    /// Write a single report block. On input, imageOffset specifies the
    /// starting point in the image. On exit, it will contain the offset for
    /// the next iteration. This method returns true to indicate there is
    /// indeed another iteration needed, false when complete.
    /// </summary>
    private bool WriteBlock(ref uint          imageOffset,
                            ref UploadResults uploadResult)
    {
        // Determine if the image at the specified block is empty. This never
        // applies to the first block.
        var firstBlock = imageOffset == 0;
        var empty =      !firstBlock;

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
        if ( !empty )
        {
            Reset();

            if ( firstBlock )
            {
                Teensy.ProvideFeedback(
                    $"Erasing {Constants.TeensyWord} Flash Memory");
            }

            // Copy address bytes to report.
            var address = BitConverter.GetBytes((int)imageOffset);

            switch ( Teensy.TeensyType )
            {
                case TeensyTypes.Teensy2PlusPlus:
                {
                    AddData(address[1]);
                    AddData(address[2]);
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
                    AddData(address[0]);
                    AddData(address[1]);
                    AddData(address[2]);
                    break;
                }
            }
            
            // Copy data to report, starting at Teensy.DataOffset.
            SetDataStart(Teensy);
            
            while ( imageOffset < Image.Data.Length )
            {
                if ( !AddData(Image.Data[imageOffset]) )
                {
                    break;
                }

                ++imageOffset;
            }

            if ( Write() )
            {
                Teensy.ProvideFeedback(imageOffset,
                                       (uint)Image.Data.Length);

                // The first write erases the chip and needs a little
                // longer to complete. Allow it 5 seconds. After that, use
                // 1/2 second. This is taken from the code for teensy
                // loader at:
                // https://github.com/PaulStoffregen/teensy_loader_cli/blob/master/teensy_loader_cli.c
                Thread.Sleep(firstBlock ? 5000 : 500);
            }
            else
            {
                uploadResult = UploadResults.ErrorUpload;
            }
        }

        return !empty &&
               uploadResult == UploadResults.Success &&
               imageOffset < Image.Data.Length;
    }
}

}
