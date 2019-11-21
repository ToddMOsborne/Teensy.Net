namespace TeensyNet
{

using System;

/// <summary>
/// TeensyExceptions are the only onces thrown by the Teensy.Net assembly.
/// </summary>
public class TeensyException : Exception
{
    /// <summary>
    /// Constructor must be initialized with the message.
    /// </summary>
    internal TeensyException(string message) : base(message)
    {
    }

    internal TeensyException(string    message,
                             Exception innerException) : base(message,
                                                              innerException)
    {
    }
}

}
