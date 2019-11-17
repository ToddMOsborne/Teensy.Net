namespace Teensy.Net
{

using System;
using System.IO;

/// <summary>
/// HexImage represents the software (firmware) that is uploaded to a Teensy
/// device.
/// </summary>
public class HexImage
{
    /// <summary>
    /// Constructor must be initialized with the Teensy and the reader to read
    /// image data from. Use Factory.LastException to find any errors.
    /// </summary>
    public HexImage(Teensy     teensy,
                    TextReader reader,
                    bool       disposeStream = false)
    {
        Teensy = teensy;

        // Initialize image.
        var size = Teensy.FlashSize;
        Data =     new byte[size];

        for ( var i = 0; i < size; i++ )
        {
            Data[i] = 0xFF;
        }

        var lineNumber = (uint)1;

        // Upper Linear Base Address.
        var ulba = (uint)0;

        // Segment Base Address.
        var segba = (uint)0;

        // Do not allow parsing to throw exception.
        Teensy.Factory.SafeMethod( () =>
        {
            var tempData = new byte[0];

            while ( ParseLine(reader.ReadLine(),
                              ref ulba,
                              ref segba,
                              ref tempData) )
            {
                ++lineNumber;
            }

        }, out var exception);

        if ( disposeStream )
        {
            Teensy.Factory.SafeMethod( () =>
            {
                reader.Close();
                reader.Dispose();

            }, false);
        }

        if ( exception != null )
        {
            Teensy.Factory.LastException = new HexImageException(
                $"Invalid hex file image data was found on or near line {lineNumber}: {exception.Message}");
        }
    }

    /// <summary>
    /// Constructor must be initialized with the Teensy and the name of the
    /// hex file to read. Use Factory.LastException to find any errors.
    /// However, this will throw a FileNotFoundException if hexFileName is
    /// invalid.
    /// </summary>
    public HexImage(Teensy teensy,
                    string hexFileName) : this(teensy,
                                               File.OpenText(hexFileName),
                                               true)
    {
    }

    /// <summary>
    /// Call a callback once for each chunk of data in the image. The callback
    /// will receive the data as well as the offset into the data. Return true
    /// from the callback to keeping chunking.
    /// </summary>
    public void Chunk(uint                     chunkSize,
                      Func<byte[], uint, bool> callback)
    {
        var chunk =       new byte[chunkSize];
        var imageData =   Data;
        var imageLength = imageData.Length;

        for ( var imageOffset = 0u;
              imageOffset < imageLength;
              imageOffset += chunkSize )
        {
            var keepGoing = true;

            // Clear chunk.
            for ( var i = 0; i < chunkSize; i++ )
            {
                chunk[i] = 0;
            }

            // Is the image empty?
            for ( var i = 0;
                  i < chunkSize && imageOffset + i < imageLength;
                  i++ )
            {
                keepGoing = false;

                // Not empty? Always call for first chunk.
                if ( imageOffset == 0 || imageData[imageOffset + i] != 0xFF )
                {
                    // Copy data.
                    for ( var j = 0;
                          j < chunkSize && imageOffset + j < imageLength;
                          j++ )
                    {
                        chunk[j] = imageData[imageOffset + j];
                    }

                    keepGoing = callback(chunk, imageOffset);
                    break;
                }
            }

            if ( !keepGoing )
            {
                break;
            }
        }
    }

    /// <summary>
    /// Get the raw (binary) data associated with this image.
    /// </summary>
    private byte[] Data { get; }

    /// <summary>
    /// Determine if the image is valid and has been processed normally.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Try to determine if this hex image is really valid for the Teensy. This
    /// is an extra safety check that can be performed before uploading the
    /// image. This will only check Teensy 3.x series MCUs, including the LC.
    /// This is not foolproof. If it returns true, you can trust that the image
    /// is for the Teensy specified during construction. However, false does
    /// not mean this image is invalid, only that this property could not make
    /// that determination. You may want to warn your users before attempting
    /// to upload this image.
    /// </summary>
    public bool IsValidForTeensy
    {
        get 
        {
            var result = IsValid;

            if ( result )
            {
                // Assume failure now.
                result = false;
                
                const int start = 0x400;

                if ( Data.Length >= start )
                {
                    var resetHandlerAddress = BitConverter.ToUInt32(Data, 4);

                    if ( resetHandlerAddress < start )
                    {
                        var magic = 0;

                        switch ( resetHandlerAddress )
                        {
                            // Teensy 3.0
                            case 0xF9:
                            {
                                magic = 0x00043F82;
                                break;
                            }

                            // Teensy 3.1/2
                            case 0x1BD:
                            {
                                magic = 0x00043F82;
                                break;
                            }

                            // Teensy LC
                            case 0xC1:
                            {
                                magic = 0x00003F82;
                                break;
                            }

                            // Teensy 3.5
                            case 0x199:
                            {
                                magic = 0x00043F82;
                                break;
                            }

                            // Teensy 3.6
                            case 0x1D1:
                            {
                                magic = 0x00043F82;
                                break;
                            }
                        }

                        if ( magic > 0 )
                        {
                            for ( var offset = resetHandlerAddress;
                                  offset < start;
                                  offset++ )
                            {
                                if ( BitConverter.ToUInt32(
                                       Data,
                                       (int)offset) == magic )
                                {
                                    result = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Parse a single line. Returns true to continue processing, false
    /// otherwise. See https://en.wikipedia.org/wiki/Intel_HEX for details
    /// about the file format.
    /// </summary>
    private bool ParseLine(string     line,
                           ref uint   ulba,
                           ref uint   segba,
                           ref byte[] tempData)
    {
        // Bail?
        if ( string.IsNullOrEmpty(line) )
        {
            // End of file not found?
            if ( !IsValid )
            {
                throw new Exception("The 'end of file' marker was not found.");
            }

            return false;
        }

        if ( line.Length < 11 || line[0] != ':' )
        {
            throw new Exception(
                "The minimum line length is 11 characters and the line must start with a colon.");
        }

        var length = Convert.ToUInt16(line.Substring(1, 2), 16);

        if ( line.Length != 11 + 2 * length )
        {
            throw new Exception("The record length is incorrect.");
        }

        var address = Convert.ToUInt16(line.Substring(3, 4), 16);
        var type =    Convert.ToUInt16(line.Substring(7, 2));

        // Copy data and calculate the checksum.
        if ( tempData.Length != length )
        {
            tempData = new byte[length];
        }

        var sum = (byte)(length + type + address);
        sum +=    (byte)((address & 0xFF00) >> 8);

        for ( var i = 0; i < length; i++ )
        {
            var b =       Convert.ToByte(line.Substring(9 + i * 2, 2), 16);
            tempData[i] = b;
            sum +=        b;
        }

        sum = (byte)( ~sum + 1);

        if ( sum != Convert.ToByte(line.Substring(line.Length - 2, 2), 16) )
        {
            throw new Exception("Checksum failed.");
        }

        switch ( type )
        {
            // Data.
            case 0:
            {
                // Data Record Load Offset.
                var drlo =   Convert.ToUInt16(line.Substring(3, 4), 16);
                var offset = ulba + segba + drlo;

                if ( Data.Length < offset + tempData.Length )
                {
                    throw new Exception(
                        $"The hex file data exceeds the flash memory size of this {Constants.TeensyWord}.");
                }

                tempData.CopyTo(Data, offset);
                break;
            }

            // End of file.
            case 1:
            {
                IsValid = true;
                break;
            }

            // Extended Segment Address.
            case 2:
            {
                ulba =  0;
                segba = (256u * tempData[0] + tempData[1]) << 4;
                break;
            }

            // Extended Linear Address.
            case 4:
            {
                segba = 0;
                ulba =  (256U * tempData[0] + tempData[1]) << 16;
                
                if ( ulba == 0x6000_0000 )
                {
                    ulba = 0;
                } 

                break;
            }

            default:
            {
                throw new Exception($"Unsupported record type: {type}.");
            }
        }

        return !IsValid;
    }

    /// <summary>
    /// The Teensy this object exists for.
    /// </summary>
    public Teensy Teensy { get; }
}

}
