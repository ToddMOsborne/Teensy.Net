namespace Teensy.Net
{

using System;
using System.IO.Ports;
using System.Threading;

/// <summary>
/// A single Teensy connected via USB.
/// </summary>
public class Teensy
{
    /// <summary>
    /// Internal constructor.
    /// </summary>
    internal Teensy(TeensyFactory factory,
                    TeensyTypes   teensyType,
                    string        portName,
                    uint          serialNumber,
                    UsbTypes      usbType)
    {
        // ReSharper disable once JoinNullCheckWithUsage
        if ( factory == null )
        {
            throw new TeensyException(
                $"It is not possible to create a {Constants.TeensyWord} object without an owner TeensyFactory.");
        }

        if ( usbType == UsbTypes.Disconnected )
        {
            throw new TeensyException(
                $"It is not possible to create a {Constants.TeensyWord} object in a disconnected state.");
        }

        if ( serialNumber == 0 )
        {
            throw new TeensyException(
                $"It is not possible to create a {Constants.TeensyWord} object without a serial number.");
        }

        TeensyType =    CheckType(teensyType);
        Factory =       factory;
        _portName =     portName;
        SerialNumber =  serialNumber;
        UsbType =       usbType;
    }

    /// <summary>
    /// Change the internal state. Returns true if something actually changed.
    /// </summary>
    internal bool ChangeState(UsbTypes usbType,
                              string   portName)
    {
        var result = UsbType != usbType || _portName != portName;

        if ( result )
        {
            _portName = portName;
            
            if ( UsbType != usbType )
            {
                UsbType = usbType;
                
                switch ( usbType )
                {
                    case UsbTypes.Bootloader:
                    {
                        UsbTypeBootloaderReady.Set();

                        ProvideFeedback(
                            $"{Constants.TeensyWord} Bootloader Running");

                        break;
                    }

                    case UsbTypes.Disconnected:
                    {
                        ProvideFeedback(
                            $"{Constants.TeensyWord} Disconnected");

                        break;
                    }

                    case UsbTypes.Serial:
                    {
                        UsbTypeSerialReady.Set();
                        ProvideFeedback($"{Constants.TeensyWord} Connected");
                        break;
                    }
                }
            }

            ConnectionStateChanged?.Invoke(this);
        }

        return result;
    }

    /// <summary>
    /// Makes sure type is a support type. As long as type is valid, it will
    /// be returned. If invalid, this will throw.
    /// </summary>
    internal static TeensyTypes CheckType(TeensyTypes type)
    {
        switch ( type )
        {
            case TeensyTypes.Teensy2:
            case TeensyTypes.Teensy2PlusPlus:
            case TeensyTypes.TeensyLc:
            case TeensyTypes.Teensy30:
            case TeensyTypes.Teensy31:
            case TeensyTypes.Teensy32:
            case TeensyTypes.Teensy35:
            case TeensyTypes.Teensy36:
            case TeensyTypes.Teensy40:
            {
                return type;
            }

            default:
            {
                throw new TeensyException(
                    $"Unknown {Constants.TeensyWord} type of {type}.");
            }
        }
    }

    /// <summary>
    /// This event is fired when the UsbType or PortName changes. In other
    /// words, when a Teensy device goes from Serial to Bootloader, Bootloader
    /// to Serial, or becomes disconnected.
    /// </summary>
    public event Action<Teensy> ConnectionStateChanged;

    /// <summary>
    /// Get the size of each data block when uploading to the Teensy device.
    /// </summary>
    public uint DataBlockSize => GetDataBlockSize(TeensyType);

    /// <summary>
    /// Get the TeensyFactory object that created this Teensy.
    /// </summary>
    public TeensyFactory Factory { get; }

    /// <summary>
    /// This event is fired whenever work is being done on the Teensy.
    /// The parameters received by the event handler are this object, a string
    /// that describes what is happening, and, during uploads, integers that
    /// specify the number of bytes uploaded and the total byte count that will
    /// be uploaded.
    /// </summary>
    public event Action<Teensy, string, uint, uint> FeedbackProvided;

    /// <summary>
    /// Get the number of bytes in flash memory a Teensy has.
    /// </summary>
    public uint FlashSize => GetFlashSize(TeensyType);

    /// <summary>
    /// Get the size of each data block when uploading to the Teensy device.
    /// </summary>
    public static uint GetDataBlockSize(TeensyTypes type)
    {
        var result = 1024u;

        switch ( CheckType(type) )
        {
            case TeensyTypes.Teensy2:
            {
                result = 128;
                break;
            }

            case TeensyTypes.Teensy2PlusPlus:
            {
                result = 256;
                break;
            }

            case TeensyTypes.TeensyLc:
            {
                result = 512;
                break;
            }
        }

        return result;
    }

    /// <summary>
    /// Get the number of bytes of flash memory a Teensy has.
    /// </summary>
    public static uint GetFlashSize(TeensyTypes type)
    {
        var result = 0u;

        switch ( CheckType(type) )
        {
            case TeensyTypes.Teensy2:
            {
                result = 32256;
                break;
            }

            case TeensyTypes.Teensy2PlusPlus:
            {
                result = 130048;
                break;
            }

            case TeensyTypes.TeensyLc:
            {
                result = 63488;
                break;
            }

            case TeensyTypes.Teensy30:
            {
                result = 131072;
                break;
            }

            case TeensyTypes.Teensy31:
            case TeensyTypes.Teensy32:
            {
                result = 262144;
                break;
            }

            case TeensyTypes.Teensy35:
            {
                result = 524288;
                break;
            }

            case TeensyTypes.Teensy36:
            {
                result = 1048576;
                break;
            }

            case TeensyTypes.Teensy40:
            {
                result = 2031616;
                break;
            }
        }

        return result;
    }

    /// <summary>
    /// Get the type of Microcontroller chip on the Teensy device.
    /// </summary>
    public static string GetMcuType(TeensyTypes type)
    {
        string result = null;

        switch ( CheckType(type) )
        {
            case TeensyTypes.Teensy2:
            {
                result = "ATMEGA32U4";
                break;
            }

            case TeensyTypes.Teensy2PlusPlus:
            {
                result = "AT90USB1286";
                break;
            }

            case TeensyTypes.TeensyLc:
            {
                result = "MKL26Z64VFT4";
                break;
            }

            case TeensyTypes.Teensy30:
            {
                result = "MK20DX128";
                break;
            }

            case TeensyTypes.Teensy31:
            {
                result = "MK20DX256";
                break;
            }

            case TeensyTypes.Teensy32:
            {
                result = "MK20DX256VLH7";
                break;
            }

            case TeensyTypes.Teensy35:
            {
                result = "MK64FX512VMD12";
                break;
            }

            case TeensyTypes.Teensy36:
            {
                result = "MK66FX1M0VMD18";
                break;
            }

            case TeensyTypes.Teensy40:
            {
                result = "iMXRT1062";
                break;
            }
        }

        return result;
    }

    /// <summary>
    /// Get the type of Microcontroller chip on the Teensy device.
    /// </summary>
    public string McuType => GetMcuType(TeensyType);

    /// <summary>
    /// Return the friendly name of this Teensy, like "Teensy 3.2".
    /// </summary>
    public string Name
    {
        get
        {
            var result = Constants.TeensyWord + ' ';

            switch ( TeensyType )
            {
                case TeensyTypes.Teensy2:
                {
                    result += "2";
                    break;
                }

                case TeensyTypes.Teensy2PlusPlus:
                {
                    result += "2++";
                    break;
                }

                case TeensyTypes.TeensyLc:
                {
                    result += "LC";
                    break;
                }

                case TeensyTypes.Teensy30:
                {
                    result += "3.0";
                    break;
                }

                case TeensyTypes.Teensy32:
                {
                    result += "3.x";
                    break;
                }

                case TeensyTypes.Teensy31:
                {
                    result += "3.1";
                    break;
                }

                case TeensyTypes.Teensy35:
                {
                    result += "3.5";
                    break;
                }

                case TeensyTypes.Teensy36:
                {
                    result += "3.6";
                    break;
                }

                case TeensyTypes.Teensy40:
                {
                    result += "4.0";
                    break;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Get the name of the port used to communicate with the Teensy. This will
    /// be null when UsbType is not UsbTypes.Serial.
    /// </summary>
    public string PortName => UsbType == UsbTypes.Serial ? _portName : null;
    private string _portName;

    /// <summary>
    /// Fires the FeedbackProvided event handlers. If status is null, a
    /// string describing the upload will be used instead.
    /// </summary>
    public void ProvideFeedback(uint   bytesUploaded,
                                uint   uploadSize,
                                string status = null)
    {
        var eh = FeedbackProvided;

        if ( eh != null )
        {
            if ( status == null )
            {
                status = bytesUploaded != uploadSize
                         ? $"Uploaded {bytesUploaded} of {uploadSize} Bytes"
                         : $"{Constants.TeensyWord} Upload Complete";
            }

            Utility.Try( () =>
            {
                eh(this, status, bytesUploaded, uploadSize);
            });
        }
    }

    /// <summary>
    /// Provide feedback that is just a string.
    /// </summary>
    public void ProvideFeedback(string status) =>
        ProvideFeedback(0, 0, status);

    /// <summary>
    /// Reboot the Teensy. This method will start the bootloader as needed.
    /// </summary>
    public void Reboot()
    {
        // Bootloader must be running.
        using ( var device = StartBootloader(true) )
        {
            Reboot(device);
        }
    }

    /// <summary>
    /// Used when HidDevice is already known.
    /// </summary>
    private void Reboot(HidDevice device)
    {
        ProvideFeedback($"{Constants.TeensyWord} Rebooting");

        new HidRebootReport(device).Reboot();
        WaitForUsbTypeChange(UsbTypes.Serial);
    }

    /// <summary>
    /// Get the serial number.
    /// </summary>
    public uint SerialNumber { get; }

    /// <summary>
    /// Start the bootloader, as needed. A Reboot() is required to get out of
    /// bootloader mode.
    /// </summary>
    public void StartBootloader() => StartBootloader(false);

    /// <summary>
    /// Start the bootloader, as needed, and return the HidDevice for it, if
    /// desired.
    /// </summary>
    private HidDevice StartBootloader(bool returnHidDevice)
    {
        HidDevice result = null;

        // Already running?
        if ( UsbType != UsbTypes.Bootloader )
        {
            // Can't run?
            if ( PortName == null )
            {
                throw new TeensyException(
                    $"Cannot start {Constants.TeensyWord} bootloader because it is not assigned to a communications port.");
            }
            
            ProvideFeedback($"Starting {Constants.TeensyWord} Bootloader");

            Utility.Try( () =>
            {
                using ( var port = new SerialPort(PortName) )
                {
                    port.Open();
                    port.BaudRate = (int)Constants.MagicBaudRate;
                }
            }, $"Cannot start {Constants.TeensyWord} bootloader because the communications port failed to initialize with the required baud rate.");

            WaitForUsbTypeChange(UsbTypes.Bootloader);
        }

        if ( returnHidDevice )
        {
            result = HidDevice.FindDevice(SerialNumber);

            if ( result == null )
            {
                throw new TeensyException(
                    $"Failed to find the {Constants.TeensyWord} device.");
            }
        }

        return result;
    }

    /// <summary>
    /// Get the type of Teensy.
    /// </summary>
    public TeensyTypes TeensyType { get; }

    /// <summary>
    /// This timeout is used when starting the bootloader, reboots, etc. The
    /// default, and minimum, is 5 seconds.
    /// </summary>
    public TimeSpan Timeout
    {
        get => _timeout;
        set 
        {
            if ( value.TotalSeconds < 5f )
            {
                throw new TeensyException(
                    $"The minimum Timeout value for a {Constants.TeensyWord} device is 5 seconds.");
            }

            _timeout = value;
        }

    }
    private TimeSpan _timeout = new TimeSpan(0, 0, 5);

    /// <summary>
    /// Upload the image to the Teensy. A reboot after upload is required to
    /// bring the Teensy back online, and will be done automatically by this
    /// method.
    /// </summary>
    public void UploadImage(string hexFileName) =>
        UploadImage(new HexImage(TeensyType, hexFileName));

    /// <summary>
    /// Upload the image to the Teensy. A reboot after upload is required to
    /// bring the Teensy back online, and will be done automatically by this
    /// method.
    /// </summary>
    public void UploadImage(HexImage image)
    {
        if ( image == null )
        {
            throw new TeensyException(
                "The HEX image to upload must be specified.");
        }

        // Make sure the bootloader is running.
        using ( var device = StartBootloader(true) )
        {
            new HidUploadReport(device, this, image).Upload();
            Reboot(device);
        }
    }

    /// <summary>
    /// Returns friendly name for device. When the bootloader is running this
    /// will return "Bootloader for {Name} Serial Number {SerialNumber}.
    /// When connected via serial this will return
    /// "{Name} Serial Number {SerialNumber} on {PortName}". Any other state
    /// will return "Disconnected {Name} Serial Number {SerialNumber}".
    /// </summary>
    public override string ToString()
    {
        string result;

        switch ( UsbType )
        {
            case UsbTypes.Bootloader:
            {
                result = $"Bootloader for {Name} Serial Number {SerialNumber}";
                break;
            }

            case UsbTypes.Serial:
            {
                result = $"{Name} Serial Number {SerialNumber} on {PortName}";
                break;
            }

            default:
            {
                result = $"Disconnected {Name} Serial Number {SerialNumber}";
                break;
            }
        }

        return result;
    }

    /// <summary>
    /// Get the USB type.
    /// </summary>
    public UsbTypes UsbType { get; private set; }

    /// <summary>
    /// Signals when bootloader is ready.
    /// </summary>
    private ManualResetEvent UsbTypeBootloaderReady { get; } =
        new ManualResetEvent(true);

    /// <summary>
    /// Signals when serial is ready.
    /// </summary>
    private ManualResetEvent UsbTypeSerialReady { get; } =
        new ManualResetEvent(true);

    /// <summary>
    /// Wait for a change in the UsbType.
    /// </summary>
    private void WaitForUsbTypeChange(UsbTypes desiredType)
    {
        if ( desiredType == UsbTypes.Disconnected )
        {
            throw new TeensyException("Cannot wait for disconnected state.");
        }

        var e = desiredType == UsbTypes.Bootloader
                ? UsbTypeBootloaderReady
                : UsbTypeSerialReady;

        e.Reset();

        if ( !e.WaitOne(Timeout) )
        {
            throw new TeensyException(
                $"Failed waiting for the {Constants.TeensyWord} device state to change to {desiredType}.");
        }
    }
}

}
