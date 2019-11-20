namespace Teensy.Net
{

using System.Threading;

#if DEBUG
    using System.IO;
#endif

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

        // The block size must be less than the HID report length.
        if ( teensy.DataBlockSize > device.ReportLength - 1 )
        {
            throw new TeensyException(
                $"The data block size of a {Constants.TeensyWord} must be smaller than the HID report size, including the first byte, which is the report ID.");
        }
    }

    /// <summary>
    /// The HexImage.
    /// </summary>
    private HexImage Image { get; }

    /// <summary>
    /// The Teensy object.
    /// </summary>
    private Teensy Teensy { get; }

    #if DEBUG
        /// <summary>
        /// Used for debugging only. This causes the upload report output
        /// normally sent to the Teensy to be directed to this file instead.
        /// </summary>
        public FileStream TestOutputStream { get; private set; }
    #endif

    /// <summary>
    /// Upload an image to the Teensy.
    /// </summary>
    public void Upload()
    {
        #if DEBUG
            // ReSharper disable HeuristicUnreachableCode
            #pragma warning disable 162
            const string filePath = null;
            //const string filePath = "T:\\Source\\Teensy.Net\\TestFiles\\blinkLC.hex.TeensyNetOutput";
            //const string filePath = "T:\\Source\\Teensy.Net\\TestFiles\\blink32.hex.TeensyNetOutput";

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if ( filePath != null )
            {
                TestOutputStream = File.Open(filePath,
                                             FileMode.Create,
                                             FileAccess.Write,
                                             FileShare.None);
            }
            #pragma warning restore 162
            // ReSharper restore HeuristicUnreachableCode
        #endif

        Image.Chunk((bytes, imageOffset) =>
        {
            // The data offset is how much free space to leave in the HID
            // report data before writing of actual image data. The address
            // is always first, but writing image data should occur at this
            // offset.
            var dataOffset = 2u;

            // Add address (image offset).
            if ( Teensy.DataBlockSize <= 256 && Teensy.FlashSize < 0x10000 )
            {
                AddData((byte)(imageOffset & 0xFF));
                AddData((byte)((imageOffset >> 8) & 0xFF));
            }
            else if ( Teensy.DataBlockSize == 256 )
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

            Write();

            // The first write erases the chip and needs a little
            // longer to complete.
            Thread.Sleep(imageOffset == 0 ? 3000 : 500);

            Teensy.ProvideFeedback(imageOffset + Teensy.DataBlockSize,
                                   Image.Size);
        });

        #if DEBUG
            if ( TestOutputStream != null )
            {
                TestOutputStream.Close();
                TestOutputStream.Dispose();
                TestOutputStream = null;
            }
        #endif
    }
}

}
