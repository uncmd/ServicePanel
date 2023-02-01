using System.Runtime.InteropServices;

namespace ServicePanel.Agent.Statistics;

internal partial class CPUHelper
{
    public static CPUTime GetCPUTime()
    {
        FILETIME lpIdleTime = default;
        FILETIME lpKernelTime = default;
        FILETIME lpUserTime = default;
        if (!GetSystemTimes(out lpIdleTime, out lpKernelTime, out lpUserTime))
        {
            return default;
        }
        return GetCPUTime(lpIdleTime, lpKernelTime, lpUserTime);
    }

    /// <summary>
    /// 计算 CPU 使用率
    /// </summary>
    /// <param name="oldTime"></param>
    /// <param name="newTime"></param>
    /// <returns></returns>
    public static double CalculateCPULoad(CPUTime oldTime, CPUTime newTime)
    {
        ulong totalTicksSinceLastTime = newTime.SystemTime - oldTime.SystemTime;
        ulong idleTicksSinceLastTime = newTime.IdleTime - oldTime.IdleTime;

        double ret = 1.0f - ((totalTicksSinceLastTime > 0) ? ((double)idleTicksSinceLastTime) / totalTicksSinceLastTime : 0);

        return ret;
    }

    /// <summary>
    /// 获取 CPU 工作时间
    /// </summary>
    /// <param name="lpIdleTime"></param>
    /// <param name="lpKernelTime"></param>
    /// <param name="lpUserTime"></param>
    /// <returns></returns>
    private static CPUTime GetCPUTime(FILETIME lpIdleTime, FILETIME lpKernelTime, FILETIME lpUserTime)
    {
        var IdleTime = ((ulong)lpIdleTime.DateTimeHigh << 32) | lpIdleTime.DateTimeLow;
        var KernelTime = ((ulong)lpKernelTime.DateTimeHigh << 32) | lpKernelTime.DateTimeLow;
        var UserTime = ((ulong)lpUserTime.DateTimeHigh << 32) | lpUserTime.DateTimeLow;

        var SystemTime = KernelTime + UserTime;

        return new CPUTime(IdleTime, SystemTime);
    }

    /*
    IdleTime 空闲时间
    KernelTime 内核时间
    UserTime 用户时间

    系统时间 = 内核时间 + 用户时间
    SystemTime = KernelTime + UserTime

     */

    /// <summary>
    /// 在多处理器系统上，返回的值是所有处理器指定时间的总和
    /// </summary>
    /// <remarks><see href="https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getsystemtimes"/></remarks>
    /// <param name="lpIdleTime">指向 FILETIME 结构的指针，该结构接收系统空闲的时间量</param>
    /// <param name="lpKernelTime">指向 FILETIME 结构的指针，该结构接收系统在内核模式下执行的时间量(包括所有进程中的所有线程以及所有处理器上的所有线程)。此时间值还包括系统空闲的时间</param>
    /// <param name="lpUserTime">指向 FILETIME 结构的指针，该结构接收系统在 User 模式下执行的时间量(包括所有进程中的所有线程以及所有处理器上的所有线程)</param>
    /// <returns></returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);
}

internal struct CPUTime
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="idleTime"></param>
    /// <param name="systemTime"></param>
    public CPUTime(ulong idleTime, ulong systemTime)
    {
        IdleTime = idleTime;
        SystemTime = systemTime;
    }

    /// <summary>
    /// CPU 空闲时间
    /// </summary>
    public ulong IdleTime { get; private set; }

    /// <summary>
    /// CPU 工作时间
    /// </summary>
    public ulong SystemTime { get; private set; }
}

[StructLayout(LayoutKind.Sequential)]
internal struct FILETIME
{
    /// <summary>
    /// 时间的低位部分
    /// </summary>
    public uint DateTimeLow;

    /// <summary>
    /// 时间的高位部分
    /// </summary>
    public uint DateTimeHigh;
}
