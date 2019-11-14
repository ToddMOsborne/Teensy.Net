namespace Teensy.Net
{

using System;

/// <summary>
/// HexImageException are thrown when working with HexImages, which are used
/// for uploading software (firmware) to the Teensy.
/// </summary>
public class HexImageException : Exception
{
    /// <summary>
    /// Constructor must be initialized with the message.
    /// </summary>
    public HexImageException(string message) : base(message)
    {
    }
}

}
