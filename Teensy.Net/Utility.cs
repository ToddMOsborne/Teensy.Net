namespace Teensy.Net
{

using System;

/// <summary>
/// Static helper methods.
/// </summary>
public static class Utility
{
    /// <summary>
    /// If serialNumber is not invalid, multiply be 10.
    /// </summary>
    internal static uint FixSerialNumber(uint serialNumber)
    {
        if ( serialNumber != 0xFFFFFFFF )
        {
            serialNumber *= 10;
        }

        return serialNumber;
    }

    /// <summary>
    /// If e is null, do nothing. If not null and a TeensyException, just throw
    /// it. If not a TeensyException, wrap in TeensyException and throw.
    /// </summary>
    public static void Throw(Exception e,
                             string    message)
    {
        if ( e != null )
        {
            if ( !(e is TeensyException) )
            {
                e = new TeensyException(message, e);
            }

            throw e;
        }
    }

    /// <summary>
    /// Execute a method. If it throws an exception, return it.
    /// </summary>
    public static Exception Try(Action callback)
    {
        Exception result = null;

        try
        {
            callback();
        }
        catch(Exception e)
        {
            result = e;
        }

        return result;
    }

    /// <summary>
    /// Execute a method. If it throwns an exception, rethrow as
    /// TeensyException. If already a TeensyException, just rethrow as-is.
    /// </summary>
    public static void Try(Action callback,
                           string teensyExceptionMessage) =>
        Throw(Try(callback), teensyExceptionMessage);
}

}