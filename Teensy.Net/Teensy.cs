namespace Teensy.Net
{

using System;
using System.IO;
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
            throw new InvalidOperationException(
                $"It is not possible to create a {Constants.TeensyWord} object without an owner TeensyFactory.");
        }

        if ( teensyType == TeensyTypes.Unknown )
        {
            throw new InvalidOperationException(
                $"It is not possible to create a {Constants.TeensyWord} object without a known type.");
        }

        if ( usbType == UsbTypes.Disconnected )
        {
            throw new InvalidOperationException(
                $"It is not possible to create a {Constants.TeensyWord} object in a disconnected state.");
        }

        if ( serialNumber == 0 )
        {
            throw new InvalidOperationException(
                $"It is not possible to create a {Constants.TeensyWord} object without a serial number.");
        }

        Factory =      factory;
        TeensyType =   teensyType;
        _portName =    portName;
        SerialNumber = serialNumber;
        UsbType =      usbType;

        // Defaults that may be changed later.
        BlockSize =     1024;
        DataOffset =    64;

        switch ( teensyType )
        {
            case TeensyTypes.Teensy40:
            {
                FlashSize = 2048 * 1024;
                McuType =   "IMXRT1062";
                break;
            }

            case TeensyTypes.Teensy36:
            {
                FlashSize = 1024 * 1024;
                McuType =   "MK66FX1M0";
                break;
            }

            case TeensyTypes.Teensy35:
            {
                FlashSize = 512 * 1024;
                McuType =   "MK64FX512";
                break;
            }

            case TeensyTypes.Teensy32:
            case TeensyTypes.Teensy31:
            {
                FlashSize = 256 * 1024;
                McuType =   "MK20DX256";
                break;
            }

            case TeensyTypes.Teensy30:
            {
                FlashSize = 128 * 1024;
                McuType =   "MK20DX128";
                break;
            }

            case TeensyTypes.TeensyLc:
            {
                BlockSize = 512;
                FlashSize = 62 * 1024;
                McuType =   "MK126Z64";
                break;
            }

            case TeensyTypes.Teensy2PlusPlus:
            {
                BlockSize =  256;
                DataOffset = 2;
                FlashSize =  12 * 1024;
                McuType =    "AT90USB1286";
                break;
            }

            case TeensyTypes.Teensy2:
            {
                BlockSize =  128;
                DataOffset = 2;
                FlashSize =  31 * 1024;
                McuType =    "ATMEGA32U4";
                break;
            }
        }
    }

    /// <summary>
    /// Get the upload block size.
    /// </summary>
    internal uint BlockSize { get; }

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
    /// This event is fired when the UsbType or PortName changes.
    /// </summary>
    public event Action<Teensy> ConnectionStateChanged;

    /// <summary>
    /// Get the upload data offset.
    /// </summary>
    internal uint DataOffset { get; }

    /// <summary>
    /// Get the TeensyFactory object that created this Teensy.
    /// </summary>
    public TeensyFactory Factory { get; }

    /// <summary>
    /// This event is fired whenever work is being done on the Teensy.
    /// The parameters received by the event handler are this object, a string
    /// that describes what is happening, and during uploads, integers that
    /// specify the number of bytes uploaded and the total byte count that will
    /// be uploaded.
    /// </summary>
    public event Action<Teensy, string, uint, uint> FeedbackProvided;

    /// <summary>
    /// Get the flash memory size.
    /// </summary>
    public uint FlashSize { get; }

    /// <summary>
    /// Get the type of Microcontroller chip on the Teensy device.
    /// </summary>
    public string McuType { get; }

    /// <summary>
    /// Return the friendly name of this Teensy, like "Teeny 3.2".
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

                default:
                {
                    result = "Unknown";
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
    public string PortName =>
        UsbType == UsbTypes.Serial ? _portName : null;

    private string _portName;

    /// <summary>
    /// Fires the FeedbackProvided event handlers. If status is null, a
    /// string describing the upload will be used instead.
    /// </summary>
    internal void ProvideFeedback(uint   bytesUploaded,
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

            Factory.SafeMethod( () =>
            {
                eh(this, status, bytesUploaded, uploadSize);

            }, false);
        }
    }

    /// <summary>
    /// Provide feedback that is just a string.
    /// </summary>
    internal void ProvideFeedback(string status) =>
        ProvideFeedback(0, 0, status);

    /// <summary>
    /// Reboot the Teensy. This method will start the bootloader as needed.
    /// </summary>
    public bool Reboot()
    {
        // Bootloader must be running.
        var result = StartBootloader();

        if ( result )
        {
            Factory.SafeMethod( () =>
            {
                using ( var device =
                    HidDevice.FindDevice(SerialNumber) )
                {
                    result = device != null && Reboot(device);
                }
            });
        }

        return result;
    }

    /// <summary>
    /// Used when TeensyBootloaderDevice is already known.
    /// </summary>
    private bool Reboot(HidDevice device)
    {
        ProvideFeedback($"{Constants.TeensyWord} Rebooting");

        var result = device.Write(new HidRebootReport());

        if ( result )
        {
            result = WaitForUsbTypeChange(UsbTypes.Serial);
        }

        return result;
    }

    /// <summary>
    /// Get the serial number.
    /// </summary>
    public uint SerialNumber { get; }

    /// <summary>
    /// Start the bootloader, as needed. Returns true if bootloader
    /// is/was running. A Reboot() is required to get out of bootloader mode.
    /// </summary>
    public bool StartBootloader()
    {
        var result = UsbType == UsbTypes.Bootloader;

        // Bootloader already running, or can't run?
        if ( !result && UsbType == UsbTypes.Serial && PortName != null )
        {
            ProvideFeedback($"Starting {Constants.TeensyWord} Bootloader");

            Factory.SafeMethod( () =>
            {
                using ( var port = new SerialPort(PortName) )
                {
                    port.Open();
                    port.BaudRate = (int)Constants.MagicBaudRate;
                }

                result = WaitForUsbTypeChange(UsbTypes.Bootloader);
            });
        }

        return result;
    }

    /// <summary>
    /// Get the type of Teensy.
    /// </summary>
    public TeensyTypes TeensyType { get; }

    /// <summary>
    /// This timeout is used when starting the bootloader, reboots, etc. The
    /// default is 5 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = new TimeSpan(0, 0, 5);

    /// <summary>
    /// Upload the image to the Teensy. A reboot after upload is required to
    /// bring the Teensy back online, and will be done automatically by this
    /// method.
    /// </summary>
    public UploadResults UploadImage(string hexFileName)
    {
        var result = UploadResults.ErrorInvalidHexImage;

        if ( !string.IsNullOrWhiteSpace(hexFileName) &&
             File.Exists(hexFileName) )
        {
            result = UploadImage(new HexImage(this, hexFileName));
        }

        return result;
    }

    /// <summary>
    /// Upload the image to the Teensy. A reboot after upload is required to
    /// bring the Teensy back online, and will be done automatically by this
    /// method.
    /// </summary>
    public UploadResults UploadImage(HexImage image)
    {
        var result = UploadResults.ErrorInvalidHexImage;

        if ( image.IsValid )
        {
            result = UploadResults.ErrorBootLoaderNotAvailable;
            
            if ( StartBootloader() )
            {
                result = UploadResults.ErrorFindTeensy;
                
                Factory.SafeMethod( () =>
                {
                    using ( var device =
                        HidDevice.FindDevice(SerialNumber) )
                    {
                        if ( device != null )
                        {
                            result = device.Upload(this, image);

                            // Always reboot now.
                            var rebooted = Reboot(device);

                            if ( result == UploadResults.Success && !rebooted )
                            {
                                result = UploadResults.SuccessFailedReboot;
                            }
                        }
                        else
                        {
                            result = UploadResults.ErrorFindTeensy;
                        }
                    }
                });
            }
        }

        return result;
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
    private bool WaitForUsbTypeChange(UsbTypes desiredType)
    {
        if ( desiredType == UsbTypes.Disconnected )
        {
            throw new Exception("Cannot wait for disconnected state.");
        }

        var e = desiredType == UsbTypes.Bootloader
                ? UsbTypeBootloaderReady
                : UsbTypeSerialReady;

        e.Reset();
        return e.WaitOne(Timeout);
    }
}

}
