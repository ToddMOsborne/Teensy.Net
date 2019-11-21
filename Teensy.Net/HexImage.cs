namespace TeensyNet
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
    /// Get the raw (binary) data associated with this image.
    /// </summary>
    private readonly byte[] _data;

    /// <summary>
    /// Constructor must be initialized with the Teensy type and the reader to
    /// read image data from.
    /// </summary>
    public HexImage(TeensyTypes type,
                    TextReader  reader,
                    bool        disposeStream = false)
    {
        if ( reader == null )
        {
            throw new TeensyException(
                "The HEX stream to read must be specified.");
        }

        TeensyType = Teensy.CheckType(type);

        switch ( TeensyType )
        {
            case TeensyTypes.Teensy2:
            {
                DataBlockSize = 128;
                break;
            }

            case TeensyTypes.Teensy2PlusPlus:
            {
                DataBlockSize = 256;
                break;
            }

            case TeensyTypes.TeensyLc:
            {
                DataBlockSize = 512;
                break;
            }

            default:
            {
                DataBlockSize = 1024;
                break;
            }
        }

        // Initialize image.
        var valid = false;
        var size =  Teensy.GetFlashSize(TeensyType);
        _data =     new byte[size];

        for ( var i = 0; i < size; i++ )
        {
            _data[i] = 0xFF;
        }

        var lineNumber = (uint)1;

        // Upper Linear Base Address.
        var ulba = (uint)0;

        // Segment Base Address.
        var segba = (uint)0;

        // Do not allow parsing to throw exception. We may need to dispose of
        // the stream after.
        var exception = Utility.Try( () =>
        {
            var tempData = new byte[0];

            while ( ParseLine(reader.ReadLine(),
                              ref ulba,
                              ref segba,
                              ref tempData,
                              ref valid) )
            {
                ++lineNumber;
            }
        });

        if ( disposeStream )
        {
            Utility.Try(reader.Dispose);
        }

        if ( exception != null )
        {
            Utility.Throw(
                exception,
                $"Invalid HEX file image data was found on or near line {lineNumber}.");
        }

        // Determine the total length of data that will be written.
        var totalLength = 0u;

        Chunk((bytes, imageOffset) =>
        {
            totalLength = imageOffset + DataBlockSize;
        });

        // Reset Data to match actual size.
        Array.Resize(ref _data, (int)totalLength);
    }

    /// <summary>
    /// Constructor must be initialized with the Teensy type and the name of
    /// the hex file to read.
    /// </summary>
    public HexImage(TeensyTypes type,
                    string      hexFileName) : this(type,
                                                    CheckFile(hexFileName),
                                                    true)
    {
    }

    /// <summary>
    /// Private helper.
    /// </summary>
    private static StreamReader CheckFile(string hexFileName)
    {
        StreamReader result = null;
        
        if ( string.IsNullOrWhiteSpace(hexFileName) )
        {
            throw new TeensyException(
                "The full path to the HEX file must be specified.");
        }

        var exists = false;

        Utility.Try( () =>
        {
            exists = File.Exists(hexFileName);
        });

        if ( !exists )
        {
            throw new TeensyException(
                "The specified HEX file does not exist.");
        }

        Utility.Try( () =>
        {
            result = File.OpenText(hexFileName);
        }, "The specified HEX file could not be opened.");

        return result;
    }

    /// <summary>
    /// Call a callback once for each chunk of data in the image. The callback
    /// will receive the data as well as the offset into the data.
    /// </summary>
    internal void Chunk(Action<byte[], uint> callback)
    {
        var chunk = new byte[DataBlockSize];

        for ( var imageOffset = 0u;
              imageOffset < _data.Length;
              imageOffset += DataBlockSize )
        {
            var keepGoing = false;

            // Clear chunk.
            for ( var i = 0; i < DataBlockSize; i++ )
            {
                chunk[i] = 0;
            }

            // Is the image empty?
            for ( var i = 0;
                  i < DataBlockSize && imageOffset + i < _data.Length;
                  i++ )
            {
                // Not empty? Always call for first chunk.
                if ( imageOffset == 0 || _data[imageOffset + i] != 0xFF )
                {
                    keepGoing = true;

                    // Copy data.
                    for ( var j = 0;
                          j < DataBlockSize && imageOffset + j < _data.Length;
                          j++ )
                    {
                        chunk[j] = _data[imageOffset + j];
                    }

                    callback(chunk, imageOffset);
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
    /// The size of each data block that can be uploaded.
    /// </summary>
    private uint DataBlockSize { get; }

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
    public bool IsKnownGood
    {
        get 
        {
            // Assume failure.
            var result = false;
            
            const int start = 0x400;

            if ( _data.Length >= start )
            {
                var resetHandlerAddress = BitConverter.ToUInt32(_data, 4);

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
                                   _data,
                                   (int)offset) == magic )
                            {
                                result = true;
                                break;
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
                           ref byte[] tempData,
                           ref bool   valid)
    {
        // Bail?
        if ( string.IsNullOrEmpty(line) )
        {
            // End of file not found?
            if ( !valid )
            {
                throw new Exception(
                    "It does not contain an 'end of file' marker.");
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

                if ( _data.Length < offset + tempData.Length )
                {
                    throw new Exception(
                        $"The data exceeds the flash memory size of this {Constants.TeensyWord}.");
                }

                tempData.CopyTo(_data, offset);
                break;
            }

            // End of file.
            case 1:
            {
                valid = true;
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

        return !valid;
    }

    /// <summary>
    /// Get the size of this image. This is the total byte count.
    /// </summary>
    public uint Size => (uint)_data.Length;

    /// <summary>
    /// The TeensyType this object exists for.
    /// </summary>
    public TeensyTypes TeensyType { get; }
}

}
