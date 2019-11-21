// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Local
namespace TeensyNet
{

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

/// <summary>
/// HID native code wrappers.
/// </summary>
internal static class HidNativeMethods
{
    /**************************************************************************
                                  CONSTANTS
    **************************************************************************/
    private  const short DIGCF_DEVICEINTERFACE = 0x10;
    private  const short DIGCF_PRESENT =         0x2;
    internal const short FILE_SHARE_READ =       0x1;
    internal const short FILE_SHARE_WRITE =      0x2;
    internal const uint  GENERIC_READ =          0x80000000;
    internal const uint  GENERIC_WRITE =         0x40000000;
    internal const uint  HIDP_STATUS_SUCCESS =   (0x0 << 28) | (0x11 << 16) | 0;
    internal const int   INVALID_HANDLE_VALUE =  -1;
    internal const uint  OPEN_EXISTING =         3;

    /**************************************************************************
                                  STRUCTURES
    **************************************************************************/
    [StructLayout(LayoutKind.Sequential)]
    internal struct HIDD_ATTRIBUTES
    {
        internal int           Size;
        internal ushort        VendorID;
        internal ushort        ProductID;
        private readonly short VersionNumber;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HIDP_CAPS
    {
        internal short           Usage;
        private readonly short   UsagePage;
        private readonly short   InputReportByteLength;
        internal short           OutputReportByteLength;
        private readonly short   FeatureReportByteLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        private readonly short[] Reserved;
        private readonly short   NumberLinkCollectionNodes;
        private readonly short   NumberInputButtonCaps;
        private readonly short   NumberInputValueCaps;
        private readonly short   NumberInputDataIndices;
        private readonly short   NumberOutputButtonCaps;
        private readonly short   NumberOutputValueCaps;
        private readonly short   NumberOutputDataIndices;
        private readonly short   NumberFeatureButtonCaps;
        private readonly short   NumberFeatureValueCaps;
        private readonly short   NumberFeatureDataIndices;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVICE_INTERFACE_DATA
    {
        internal int            Size;
        private Guid            InterfaceClassGuid;
        private readonly int    Flags;
        private readonly IntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
    private struct SP_DEVICE_INTERFACE_DETAIL_DATA
    {
        internal int    Size;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        internal string DevicePath;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVINFO_DATA
    {
        internal int    Size;
        internal Guid   ClassGuid;
        internal int    DevInst;
        internal IntPtr Reserved;
    }

    /**************************************************************************
                                WINDOWS API
    **************************************************************************/
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll")]
    internal static extern IntPtr CreateFile(string  fileName,
                                             uint    desiredAccess,
                                             uint    shareMode,
                                             IntPtr  securityAttributes,
                                             uint    creationDisposition,
                                             uint    flagsAndAttributes,
                                             IntPtr  overlapped);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool WriteFile(IntPtr    file,
                                          byte[]    buffer,
                                          uint      numberOfBytesToWrite,
                                          ref uint  numberOfBytesWritten,
                                          IntPtr    overlapped);

    /**************************************************************************
                                HID API
    **************************************************************************/
    [DllImport("hid.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

    [DllImport("hid.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool HidD_GetAttributes(
        IntPtr              deviceObject,
        ref HIDD_ATTRIBUTES attributes);

    [DllImport("hid.dll")]
    private static extern void HidD_GetHidGuid(ref Guid hidGuid);

    [DllImport("hid.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool HidD_GetPreparsedData(
        IntPtr     deviceObject,
        ref IntPtr preparsedData);

    [DllImport("hid.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool HidD_GetSerialNumberString(
        IntPtr   hidDeviceObject,
        ref byte reportBuffer,
        int      reportBufferLength);

    [DllImport("hid.dll")]
    internal static extern int HidP_GetCaps(IntPtr        preparsedData,
                                            ref HIDP_CAPS capabilities);

    /**************************************************************************
                                SETUP API
    **************************************************************************/
    [DllImport("setupapi.dll")]
    private static extern int SetupDiDestroyDeviceInfoList(
        IntPtr deviceInfoSet);

    [DllImport("setupapi.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiEnumDeviceInfo(
        IntPtr              deviceInfoSet,
        int                 memberIndex,
        ref SP_DEVINFO_DATA deviceInfoData);

    [DllImport("setupapi.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiEnumDeviceInterfaces(
        IntPtr                       deviceInfoSet,
        ref SP_DEVINFO_DATA          deviceInfoData,
        ref Guid                     interfaceClassGuid,
        int                          memberIndex,
        ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid,
                                                     string   enumerator,
                                                     int      hwndParent,
                                                     int      flags);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(
        IntPtr                              deviceInfoSet,
        ref SP_DEVICE_INTERFACE_DATA        deviceInterfaceData,
        ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData,
        int                                 deviceInterfaceDetailDataSize,
        ref int                             requiredSize,
        IntPtr                              deviceInfoData);

    [DllImport("setupapi.dll",
               CharSet = CharSet.Auto,
               EntryPoint = "SetupDiGetDeviceInterfaceDetail")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern void SetupDiGetDeviceInterfaceDetailBuffer(
        IntPtr                       deviceInfoSet,
        ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
        IntPtr                       deviceInterfaceDetailData,
        int                          deviceInterfaceDetailDataSize,
        ref int                      requiredSize,
        IntPtr                       deviceInfoData);

    /**************************************************************************
                                  CLASS
    **************************************************************************/

    // GUID for HID.
    private static readonly Guid _hidClassGuid;

    /// <summary>
    /// Static constructor.
    /// </summary>
    static HidNativeMethods()
    {
        HidD_GetHidGuid(ref _hidClassGuid);
    }

    private static SP_DEVINFO_DATA CreateDeviceInfoData()
    {
        var result = new SP_DEVINFO_DATA();

        result.Size =      Marshal.SizeOf(result);
        result.DevInst =   0;
        result.ClassGuid = Guid.Empty;
        result.Reserved =  IntPtr.Zero;

        return result;
    }
    
    /// <summary>
    /// Get list of all Teensy devices, or limit the search to a specific
    /// Teensy with the specified serial number.
    /// </summary>
    internal static List<HidDevice> GetAllDevices(
        uint serialNumber = 0)
    {
        var result =    new List<HidDevice>();
        var keepGoing = true;
        var hidClass =  _hidClassGuid;

        var deviceInfoSet = SetupDiGetClassDevs(
            ref hidClass,
            null,
            0,
            DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

        if ( deviceInfoSet.ToInt64() != INVALID_HANDLE_VALUE )
        {
            var deviceInfoData = CreateDeviceInfoData();
            var deviceIndex =    0;

            while ( keepGoing &&
                    SetupDiEnumDeviceInfo(deviceInfoSet,
                                          deviceIndex,
                                          ref deviceInfoData) )
            {
                ++deviceIndex;

                var deviceInterfaceData =  new SP_DEVICE_INTERFACE_DATA();
                var deviceInterfaceIndex = 0;

                deviceInterfaceData.Size =
                    Marshal.SizeOf(deviceInterfaceData);

                while ( keepGoing &&
                        SetupDiEnumDeviceInterfaces(deviceInfoSet,
                                                    ref deviceInfoData,
                                                    ref hidClass,
                                                    deviceInterfaceIndex,
                                                    ref deviceInterfaceData) )
                {
                    ++deviceInterfaceIndex;

                    var path = GetDevicePath(deviceInfoSet,
                                             deviceInterfaceData);

                    if ( path != null )
                    {
                        var teensy = new HidDevice(path);

                        // If not a known Teensy type, skip it.
                        if ( teensy.TeensyType != TeensyTypes.Unknown )
                        {
                            if ( serialNumber == 0 ||
                                 teensy.SerialNumber == serialNumber )
                            {
                                result.Add(teensy);
                                keepGoing = serialNumber != 0;
                            }
                        }
                        else
                        {
                            teensy.Dispose();
                        }
                    }
                }
            }

            SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }

        return result;
    }

    private static string GetDevicePath(
        IntPtr                   deviceInfoSet,
        SP_DEVICE_INTERFACE_DATA deviceInterfaceData)
    {
        string result =     null;
        var    bufferSize = 0;

        var interfaceDetail = new SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            Size = IntPtr.Size == 4 ? 4 + Marshal.SystemDefaultCharSize : 8
        };

        SetupDiGetDeviceInterfaceDetailBuffer(deviceInfoSet,
                                              ref deviceInterfaceData,
                                              IntPtr.Zero,
                                              0,
                                              ref bufferSize,
                                              IntPtr.Zero);

        if ( bufferSize > 0 )
        {
            if ( SetupDiGetDeviceInterfaceDetail(deviceInfoSet,
                                                 ref deviceInterfaceData,
                                                 ref interfaceDetail,
                                                 bufferSize,
                                                 ref bufferSize,
                                                 IntPtr.Zero) )
            {
                result = string.IsNullOrEmpty(interfaceDetail.DevicePath)
                         ? null
                         : interfaceDetail.DevicePath;
            }
        }

        return result;
    }
}

}
