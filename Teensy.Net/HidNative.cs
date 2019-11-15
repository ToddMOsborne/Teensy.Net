// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Local
namespace Teensy.Net
{

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// HID native code wrappers. Much of this is based on hidlibrary.
/// </summary>
internal static class HidNative
{
    internal const short FILE_SHARE_READ =       0x1;
    internal const short FILE_SHARE_WRITE =      0x2;
    internal const uint  GENERIC_READ =          0x80000000;
    internal const uint  GENERIC_WRITE =         0x40000000;
    internal const int   INVALID_HANDLE_VALUE =  -1;
    private  const short DIGCF_DEVICEINTERFACE = 0x10;
    private  const short DIGCF_PRESENT =         0x2;
    internal const short OPEN_EXISTING =         3;
    private  const int   SPDRP_DEVICEDESC =      0;

    [StructLayout(LayoutKind.Sequential)]
    private struct DEVPROPKEY
    {
        public Guid fmtid;
        public ulong pid;
    }

    private static DEVPROPKEY DEVPKEY_Device_BusReportedDeviceDesc =
        new DEVPROPKEY
        {
            fmtid = new Guid(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2),
            pid =   4
        };

    [StructLayout(LayoutKind.Sequential)]
    internal struct HIDD_ATTRIBUTES
    {
        internal int    Size;
        internal ushort VendorID;
        internal ushort ProductID;
        internal short  VersionNumber;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HIDP_CAPS
    {
        internal short   Usage;
        internal short   UsagePage;
        internal short   InputReportByteLength;
        internal short   OutputReportByteLength;
        internal short   FeatureReportByteLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        internal short[] Reserved;
        internal short   NumberLinkCollectionNodes;
        internal short   NumberInputButtonCaps;
        internal short   NumberInputValueCaps;
        internal short   NumberInputDataIndices;
        internal short   NumberOutputButtonCaps;
        internal short   NumberOutputValueCaps;
        internal short   NumberOutputDataIndices;
        internal short   NumberFeatureButtonCaps;
        internal short   NumberFeatureValueCaps;
        internal short   NumberFeatureDataIndices;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SECURITY_ATTRIBUTES
    {
        public int    nLength;
        public IntPtr lpSecurityDescriptor;
        public bool   bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVICE_INTERFACE_DATA
    {
        internal int   cbSize;
        private Guid   InterfaceClassGuid;
        private int    Flags;
        private IntPtr Reserved;
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
        internal int    cbSize;
        internal Guid   ClassGuid;
        internal int    DevInst;
        internal IntPtr Reserved;
    }

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
    internal static extern bool CancelIoEx(IntPtr file,
                                           IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
    internal static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr CreateFile(
        string fileName,
        uint   desiredAccess,
        int    shareMode,
        ref    SECURITY_ATTRIBUTES securityAttributes,
        int    creationDisposition,
        int    flagsAndAttributes,
        int    templateFile);

    [DllImport("hid.dll")]
    internal static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

    [DllImport("hid.dll")]
    internal static extern bool HidD_GetAttributes(
        IntPtr              deviceObject,
        ref HIDD_ATTRIBUTES attributes);

    [DllImport("hid.dll")]
    private static extern void HidD_GetHidGuid(ref Guid hidGuid);

    [DllImport("hid.dll")]
    internal static extern bool HidD_GetPreparsedData(
        IntPtr     deviceObject,
        ref IntPtr preparsedData);

    [DllImport("hid.dll", CharSet = CharSet.Unicode)]
    internal static extern bool HidD_GetSerialNumberString(
        IntPtr   hidDeviceObject,
        ref byte reportBuffer,
        int      reportBufferLength);

    [DllImport("hid.dll")]
    internal static extern int HidP_GetCaps(IntPtr        preparsedData,
                                            ref HIDP_CAPS capabilities);

    [DllImport("setupapi.dll")]
    private static extern int SetupDiDestroyDeviceInfoList(
        IntPtr deviceInfoSet);

    [DllImport("setupapi.dll")]
    private static extern bool SetupDiEnumDeviceInfo(
        IntPtr              deviceInfoSet,
        int                 memberIndex,
        ref SP_DEVINFO_DATA deviceInfoData);

    [DllImport("setupapi.dll")]
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

    [DllImport("setupapi.dll", CharSet = CharSet.Auto, EntryPoint = "SetupDiGetDeviceInterfaceDetail")]
    private static  extern bool SetupDiGetDeviceInterfaceDetailBuffer(
        IntPtr                       deviceInfoSet,
        ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
        IntPtr                       deviceInterfaceDetailData,
        int                          deviceInterfaceDetailDataSize,
        ref int                      requiredSize,
        IntPtr                       deviceInfoData);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(
        IntPtr                              deviceInfoSet,
        ref SP_DEVICE_INTERFACE_DATA        deviceInterfaceData,
        ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData,
        int                                 deviceInterfaceDetailDataSize,
        ref int                             requiredSize,
        IntPtr                              deviceInfoData);

    [DllImport("setupapi.dll", EntryPoint = "SetupDiGetDevicePropertyW", SetLastError = true)]
    private static extern bool SetupDiGetDeviceProperty(
        IntPtr              deviceInfo,
        ref SP_DEVINFO_DATA deviceInfoData,
        ref DEVPROPKEY      propkey,
        ref ulong           propertyDataType,
        byte[]              propertyBuffer,
        int                 propertyBufferSize,
        ref int             requiredSize,
        uint                flags);

    [DllImport("setupapi.dll", EntryPoint = "SetupDiGetDeviceRegistryProperty")]
    private static extern bool SetupDiGetDeviceRegistryProperty(
        IntPtr              deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        int                 propertyVal,
        ref int             propertyRegDataType,
        byte[]              propertyBuffer,
        int                 propertyBufferSize,
        ref int             requiredSize);

    // GUID for HID.
    private static readonly Guid _hidClassGuid;

    /// <summary>
    /// Static constructor.
    /// </summary>
    static HidNative()
    {
        HidD_GetHidGuid(ref _hidClassGuid);
    }

    private static SP_DEVINFO_DATA CreateDeviceInfoData()
    {
        var result = new SP_DEVINFO_DATA();

        result.cbSize =    Marshal.SizeOf(result);
        result.DevInst =   0;
        result.ClassGuid = Guid.Empty;
        result.Reserved =  IntPtr.Zero;

        return result;
    }
    
    /// <summary>
    /// Get list of all devices.
    /// </summary>
    internal static List<HidDevice> GetAllDevices()
    {
        var result =   new List<HidDevice>();
        var hidClass = _hidClassGuid;

        var deviceInfoSet = SetupDiGetClassDevs(
            ref hidClass,
            null,
            0,
            DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

        if ( deviceInfoSet.ToInt64() != INVALID_HANDLE_VALUE )
        {
            var deviceInfoData = CreateDeviceInfoData();
            var deviceIndex =    0;

            while ( SetupDiEnumDeviceInfo(deviceInfoSet,
                                          deviceIndex,
                                          ref deviceInfoData) )
            {
                ++deviceIndex;

                var deviceInterfaceData =  new SP_DEVICE_INTERFACE_DATA();
                var deviceInterfaceIndex = 0;

                deviceInterfaceData.cbSize =
                    Marshal.SizeOf(deviceInterfaceData);

                while ( SetupDiEnumDeviceInterfaces(deviceInfoSet,
                                                    ref deviceInfoData,
                                                    ref hidClass,
                                                    deviceInterfaceIndex,
                                                    ref deviceInterfaceData) )
                {
                    ++deviceInterfaceIndex;

                    var devicePath = GetDevicePath(deviceInfoSet,
                                                   deviceInterfaceData);

                    var description =
                        GetBusReportedDeviceDescription(deviceInfoSet,
                                                        ref deviceInfoData)
                        ?? GetDeviceDescription(deviceInfoSet,
                                                ref deviceInfoData);

                    result.Add(new HidDevice(description, devicePath));
                }
            }

            SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }

        return result;
    }

    private static string GetBusReportedDeviceDescription(
        IntPtr              deviceInfoSet,
        ref SP_DEVINFO_DATA devinfoData)
    {
        string result = null;
        
        if ( Environment.OSVersion.Version.Major > 5 )
        {
            ulong propertyType =      0;
            var   requiredSize =      0;
            var   descriptionBuffer = new byte[1024];

            var valid = SetupDiGetDeviceProperty(deviceInfoSet,
                                                 ref devinfoData,
                                                 ref DEVPKEY_Device_BusReportedDeviceDesc,
                                                 ref propertyType,
                                                 descriptionBuffer,
                                                 descriptionBuffer.Length,
                                                 ref requiredSize,
                                                 0);

            if ( valid )
            {
                result = Encoding.Unicode.GetString(descriptionBuffer);
                result = result.Remove(result.IndexOf((char)0));
            }
        }

        return result;
    }

    private static string GetDeviceDescription(
        IntPtr              deviceInfoSet,
        ref SP_DEVINFO_DATA devinfoData)
    {
        var descriptionBuffer = new byte[1024];
        var requiredSize =      0;
        var type =              0;

        SetupDiGetDeviceRegistryProperty(deviceInfoSet,
                                         ref devinfoData,
                                         SPDRP_DEVICEDESC,
                                         ref type,
                                         descriptionBuffer,
                                         descriptionBuffer.Length,
                                         ref requiredSize);

        var result = Encoding.UTF8.GetString(descriptionBuffer);
        result = result.Remove(result.IndexOf((char)0));

        return result;
    }

    private static string GetDevicePath(
        IntPtr                   deviceInfoSet,
        SP_DEVICE_INTERFACE_DATA deviceInterfaceData)
    {
        var bufferSize = 0;

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

        return SetupDiGetDeviceInterfaceDetail(
            deviceInfoSet,
            ref deviceInterfaceData,
            ref interfaceDetail,
            bufferSize,
            ref bufferSize,
            IntPtr.Zero) ?  interfaceDetail.DevicePath : null;
    }
}

}
