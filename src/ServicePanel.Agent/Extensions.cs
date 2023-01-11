﻿using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.ServiceProcess;

namespace ServicePanel;

public static class Extensions
{
    /// <summary>
    /// 获取服务安装文件信息
    /// </summary>
    /// <param name="serviceController"></param>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    public static Tuple<FileInfo, string> GetWindowsServiceFileInfo(this ServiceController serviceController)
    {
        string key = @"SYSTEM\CurrentControlSet\Services\" + serviceController.ServiceName;
        string imagePath = Registry.LocalMachine.OpenSubKey(key)?.GetValue("ImagePath")?.ToString();

        if (string.IsNullOrEmpty(imagePath))
        {
            return new Tuple<FileInfo, string>(null, imagePath);
        }
        else
        {
            string filePath;
            if (imagePath.StartsWith("\""))
            {
                filePath = imagePath.Split('"', StringSplitOptions.RemoveEmptyEntries)[0];
            }
            else
            {
                filePath = imagePath.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            }

            filePath = filePath.Replace("\"", string.Empty);
            imagePath = imagePath.Replace("\"", string.Empty);

            return new Tuple<FileInfo, string>(new FileInfo(filePath), imagePath);
        }
    }

    /// <summary>
    /// 获取服务PID
    /// </summary>
    /// <param name="serviceController"></param>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    public static int? GetServiceProcessId(this ServiceController serviceController)
    {
        if (serviceController == null || serviceController.Status != ServiceControllerStatus.Running)
            return null;

        IntPtr zero = IntPtr.Zero;

        try
        {
            UInt32 dwBytesNeeded;
            // Call once to figure the size of the output buffer.
            QueryServiceStatusEx(serviceController.ServiceHandle, SC_STATUS_PROCESS_INFO, zero, 0, out dwBytesNeeded);
            if (Marshal.GetLastWin32Error() == ERROR_INSUFFICIENT_BUFFER)
            {
                // Allocate required buffer and call again.
                zero = Marshal.AllocHGlobal((int)dwBytesNeeded);

                if (QueryServiceStatusEx(serviceController.ServiceHandle, SC_STATUS_PROCESS_INFO, zero, dwBytesNeeded, out dwBytesNeeded))
                {
                    var ssp = new SERVICE_STATUS_PROCESS();
                    Marshal.PtrToStructure(zero, ssp);
                    return (int)ssp.dwProcessId;
                }
            }
        }
        catch
        {
        }
        finally
        {
            if (zero != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(zero);
            }
        }
        return null;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class SERVICE_STATUS_PROCESS
    {
        [MarshalAs(UnmanagedType.U4)]
        public uint dwServiceType;
        [MarshalAs(UnmanagedType.U4)]
        public uint dwCurrentState;
        [MarshalAs(UnmanagedType.U4)]
        public uint dwControlsAccepted;
        [MarshalAs(UnmanagedType.U4)]
        public uint dwWin32ExitCode;
        [MarshalAs(UnmanagedType.U4)]
        public uint dwServiceSpecificExitCode;
        [MarshalAs(UnmanagedType.U4)]
        public uint dwCheckPoint;
        [MarshalAs(UnmanagedType.U4)]
        public uint dwWaitHint;
        [MarshalAs(UnmanagedType.U4)]
        public uint dwProcessId;
        [MarshalAs(UnmanagedType.U4)]
        public uint dwServiceFlags;
    }

    internal const int ERROR_INSUFFICIENT_BUFFER = 0x7a;
    internal const int SC_STATUS_PROCESS_INFO = 0;

    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern bool QueryServiceStatusEx(SafeHandle hService, int infoLevel, IntPtr lpBuffer, uint cbBufSize, out uint pcbBytesNeeded);
}
