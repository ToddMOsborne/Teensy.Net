﻿namespace TeensyNet
{

using System;
using System.Collections.Generic;
using System.Management;
using System.Threading;

/// <summary>
/// Builds list of connected Teensy devices and provides events and properties
/// for working with them. This is the "main brains" of the assembly and is
/// responsible for generating other objects and provides some utility
/// methods and events. It listens for the operating system notifications of
/// when Teensy devices are connected and disconnected, creating new Teensy
/// objects dynamically, and updating their state. The normal usage pattern is
/// to create one of these objects, hook into the TeensyAdded event to be
/// notifiied when new Teensy devices are connected to the system, and/or use
/// the EnumTeensies() method to discover connected Teensy devices.
/// </summary>
public class TeensyFactory : IDisposable
{
    // Private constants.
    private const uint SerialNumberId = 0x483;

    /// <summary>
    /// Internal list of Teensy devices. Access to this list must be locked.
    /// </summary>
    private readonly List<Teensy> _teensys = new List<Teensy>();
    
    /// <summary>
    /// Default constructor. If desired, the frequency of checks for updates
    /// to Teensys connected/disconnected can be specified. The default is to
    /// check every 3 seconds. One second is the minimum value that can be
    /// used.
    /// </summary>
    public TeensyFactory(TimeSpan? changeInterval = null)
    {
        if ( changeInterval.HasValue &&
             changeInterval.Value.TotalSeconds < 1.0F )
        {
            throw new TeensyException(
                $"The minimum interval to look for {Constants.TeensyWord} devices is 1 second.");
        }

        var exception = Utility.Try( () =>
        {
            var vendor = "'%USB_VID[_]" +
                         Constants.VendorId.ToString("X") + "%'";

            using ( var searcher =
                new ManagementObjectSearcher(
                    "root\\CIMV2",
                    $"SELECT PNPDeviceID,Caption,HardwareID FROM Win32_PnPEntity WHERE DeviceID LIKE {vendor}") )
            {
                foreach ( var mgmtObject in searcher.Get() )
                {
                    Utility.Try( () =>
                    {
                        var mcu = CreateTeensy(mgmtObject);

                        if ( mcu != null )
                        {
                            _teensys.Add(mcu);
                        }
                    });
                }
            }

            // Create and start a watcher.
            ManagementEventWatcher StartWatcher(bool creation)
            {
                var result = new ManagementEventWatcher
                {
                    Query = new WqlEventQuery
                    {
                        EventClassName = creation
                                         ? "__InstanceCreationEvent"
                                         : "__InstanceDeletionEvent",
                        Condition =      "TargetInstance ISA 'Win32_PnPEntity'",
                        WithinInterval = changeInterval ??
                                         new TimeSpan(0, 0, 3)
                    }
                };

                if ( creation )
                {
                    result.EventArrived += OnTeensyAdded;
                }
                else
                {
                    result.EventArrived += OnTeensyRemoved;

                }

                result.Start();

                return result;
            }

            AddWatcher =    StartWatcher(true);
            RemoveWatcher = StartWatcher(false);
        });

        if ( exception != null )
        {
            Dispose();

            throw new TeensyException(
                $"Failed to initialize {Constants.TeensyWord} factory.",
                exception);
        }
    }

    /// <summary>
    /// Teensy connection watcher.
    /// </summary>
    private ManagementEventWatcher AddWatcher { get; set; }

    /// <summary>
    /// Create a Teensy from management object, if the management object
    /// specifies Teensy information.
    /// </summary>
    private Teensy CreateTeensy(ManagementBaseObject o)
    {
        Teensy result =       null;
        var    teensyType =   TeensyTypes.Unknown;
        string portName =     null;
        var    serialNumber = (uint)0;
        var    usbType =      UsbTypes.Disconnected;

        // Safely get a field value from management object.
        T GetFieldValue<T>(string fieldName)
        {
            var value = default(T);

            Utility.Try( () =>
            {
                var obj = o[fieldName];

                if ( obj is T test )
                {
                    value = test;
                }
            });

            return value;
        }

        // Given a field that is a string array, return the index value or
        // null.
        string GetFieldPart(string fieldName,
                            int    index)
        {
            string value = null;

            var stringParts = GetFieldValue<string[]>(fieldName);

            if ( stringParts != null && index < stringParts.Length )
            {
                value = stringParts[index];
            }

            return value;
        }

        // Given a field that is a string to be split, return the index value
        // or null.
        string GetSplitField(string fieldName,
                             char[] splitOn,
                             int    index)
        {
            string value = null;

            var stringParts = GetFieldValue<string>(fieldName)?.Split(splitOn);

            if ( stringParts != null && index < stringParts.Length )
            {
                value = stringParts[index];
            }

            return value;
        }

        var parts = GetFieldValue<string>("PNPDeviceID")?.Split('\\');

        if ( parts?.Length > 2 && parts[0] == "USB" )
        {
            var id = Convert.ToUInt32(parts[1].Substring(
                parts[1].IndexOf("PID_", StringComparison.Ordinal) + 4, 4),
                16);

            if ( id == SerialNumberId )
            {
                var hwId = GetFieldPart("HardwareID", 0);

                if ( hwId != null )
                {
                    portName = GetSplitField("Caption", new[] {'(', ')'}, 1);

                    if ( portName != null )
                    {
                        serialNumber = Convert.ToUInt32(parts[2]);
                        usbType =      UsbTypes.Serial;
                        
                        switch ( hwId.Substring(hwId.IndexOf(
                             "REV_",
                             StringComparison.Ordinal) + 4, 4) )
                        {
                            case "0273":
                            {
                                teensyType = TeensyTypes.TeensyLc;
                                break;
                            }

                            case "0274":
                            {
                                teensyType = TeensyTypes.Teensy30;
                                break;
                            }

                            case "0275":
                            {
                                // There is not a unique value for Teensy 3.1
                                // so we assume 3.2.
                                teensyType = TeensyTypes.Teensy32;
                                break;
                            }

                            case "0276":
                            {
                                teensyType = TeensyTypes.Teensy35;
                                break;
                            }

                            case "0277":
                            {
                                teensyType = TeensyTypes.Teensy36;
                                break;
                            }

                            case "0279":
                            {
                                teensyType = TeensyTypes.Teensy40;
                                break;
                            }
                        }
                    }
                }
            }
            // Device is running bootloader? Have to use HID.
            else if ( id == Constants.BootloaderId )
            {
                serialNumber = Utility.FixSerialNumber(
                    Convert.ToUInt32(parts[2], 16));

                // Find Teensy running bootloader.
                using ( var teensy = HidDevice.FindDevice(serialNumber) )
                {
                    if ( teensy != null )
                    {
                        teensyType = teensy.TeensyType;
                        usbType =    UsbTypes.Bootloader;
                    }
                }
            }

            // We must know these things!
            if ( teensyType   != TeensyTypes.Unknown   &&
                 usbType      != UsbTypes.Disconnected &&
                 serialNumber != 0 )
            {
                result = new Teensy(this,
                                    teensyType,
                                    portName,
                                    serialNumber,
                                    usbType);
            }
        }

        return result;
    }

    /// <summary>
    /// Proper disposal of this object is required to stop listening for
    /// Teensy event changes.
    /// </summary>
    public void Dispose()
    {
        lock(_teensys)
        {
            _teensys.Clear();
        }

        if ( AddWatcher != null )
        {
            Utility.Try(AddWatcher.Stop);
            Utility.Try(AddWatcher.Dispose);
            AddWatcher = null;
        }

        if ( RemoveWatcher != null )
        {
            Utility.Try(RemoveWatcher.Stop);
            Utility.Try(RemoveWatcher.Dispose);
            RemoveWatcher = null;
        }
    }

    /// <summary>
    /// Enumerate all Teensy devices using a callback method. Return false to
    /// stop the enumeration. If the callback throws an exception the
    /// enumeration will also stop.
    /// </summary>
    public void EnumTeensies(Func<Teensy, bool> callback)
    {
        List<Teensy> copy;

        lock(_teensys)
        {
            copy = new List<Teensy>(_teensys);
        }

        Utility.Try( () =>
        {
            foreach ( var mcu in copy )
            {
                if ( !callback(mcu) )
                {
                    break;
                }
            }
        });
    }

    /// <summary>
    /// Find a Teensy with the specified serial number. Returns null if not
    /// found. If desired, the result can also be filtered by UsbType. If
    /// serialNumber is 0, this will return the first Teensy found. type, if
    /// present, will still be used to filter results.
    /// </summary>
    public Teensy Find(uint      serialNumber = 0,
                       UsbTypes? type = null)
    {
        Teensy result = null;

        EnumTeensies( teensy =>
        {
            if ( serialNumber == 0 || teensy.SerialNumber == serialNumber )
            {
                if ( !type.HasValue || type.Value == teensy.UsbType )
                {
                    result = teensy;
                }
            }

            return result == null;
        });

        return result;
    }

    /// <summary>
    /// Notification that a Teensy was added.
    /// </summary>
    private void OnTeensyAdded(object                sender,
                               EventArrivedEventArgs e) =>
        TeensyChanged(e, true);
    
    /// <summary>
    /// Notification that a Teensy was removed.
    /// </summary>
    private void OnTeensyRemoved(object                sender,
                                 EventArrivedEventArgs e) =>
        TeensyChanged(e, false);

    /// <summary>
    /// Teensy disconnection watcher.
    /// </summary>
    private ManagementEventWatcher RemoveWatcher { get; set; }

    /// <summary>
    /// This is called when a Teensy has been added to the system.
    /// </summary>
    public event Action<Teensy> TeensyAdded;

    /// <summary>
    /// Handle the add and remove events.
    /// </summary>
    private void TeensyChanged(EventArrivedEventArgs e,
                               bool                  added)
    {
        Utility.Try( () =>
        {
            // We may get this notification for events that are not Teensys
            // at all. We should ignore those.
            var teensy = CreateTeensy((ManagementBaseObject)
                e.NewEvent["TargetInstance"]);

            if ( teensy != null )
            {
                void ThreadProc()
                {
                    // Find existing object by serial number to see if we
                    // already know about this object. That means its state
                    // has changed.
                    var existing = Find(teensy.SerialNumber);

                    if ( existing != null )
                    {
                        // Do not allow exceptions to be unhandled.
                        Utility.Try( () =>
                        {
                            existing.ChangeState(
                                added ? teensy.UsbType : UsbTypes.Disconnected,
                                teensy.PortName);
                        });
                    }
                    // Add new Teensy?
                    else if ( added )
                    {
                        lock(_teensys)
                        {
                            _teensys.Add(teensy);
                        }

                        // Do not allow exceptions to be unhandled.
                        Utility.Try( () =>
                        {
                            TeensyAdded?.Invoke(teensy);
                        });
                    }
                }

                // Do work in background thread. Things with event handlers can
                // get very wonky if we try to do this work here.
                new Thread(ThreadProc).Start();
            }
        });
    }
}

}