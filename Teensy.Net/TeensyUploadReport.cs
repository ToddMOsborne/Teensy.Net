namespace Teensy.Net
{

using System;

/// <summary>
/// This is a TeensyReport used for uploading firmware.
/// </summary>
internal class TeensyUploadReport : TeensyReport
{
    /// <summary>
    /// Constructor must be initialized with the Teensy this report is for and
    /// the HexImage object to upload.
    /// </summary>
    public TeensyUploadReport(Teensy   teensy,
                              HexImage image)
        : base(teensy.BlockSize + teensy.DataOffset + 1)
    {
        Teensy = teensy;
        Image =  image;
    }

    private HexImage Image { get; }

    /// <summary>
    /// Set the report data for uploading part of an image.
    /// </summary>
    public void InitializeImageBlock(uint imageOffset)
    {
        // Clear report buffer.
        Initialize();

        // Copy address bytes to report.
        var address = BitConverter.GetBytes((int)imageOffset);

        // There are (mostly) common.
        Data[0] = address[0];
        Data[1] = address[1];

        switch ( Teensy.TeensyType )
        {
            case TeensyTypes.Teensy2PlusPlus:
            {
                Data[0] = address[1];
                Data[1] = address[2];
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
                Data[2] = address[2];
                break;
            }
        }
        
        // Copy data to report.
        var reportOffset = Teensy.DataOffset;
        var end =          Teensy.BlockSize + Teensy.DataOffset;
        
        while ( reportOffset < end )
        {
            Data[reportOffset] = Image.Data[imageOffset];
            
            ++imageOffset;
            ++reportOffset;
        }
    }

    /// <summary>
    /// The Teensy object.
    /// </summary>
    private Teensy Teensy { get; }
}

}
