using System.Runtime.InteropServices;

namespace ServicePanel.Agent;

internal partial class MemoryHelper
{
    public static (ulong, ulong) GetMemory()
    {
        // 检查 Windows 内核版本，是否为旧系统
        if (Environment.OSVersion.Version.Major < 5)
        {
            // https://en.wikipedia.org/wiki/List_of_Microsoft_Windows_versions");
            return default;
        }

        MemoryStatusExE memoryStatusEx = new MemoryStatusExE();
        // 初始化结构的大小
        memoryStatusEx.Init();
        // 刷新值
        if (!GlobalMemoryStatusEx(ref memoryStatusEx)) 
            return default;

        return (memoryStatusEx.ullTotalPhys, memoryStatusEx.ullAvailPhys);
    }

    /// <summary>
    /// 检索有关系统当前使用物理和虚拟内存的信息
    /// </summary>
    /// <remarks><see href="https://docs.microsoft.com/zh-cn/windows/win32/api/sysinfoapi/nf-sysinfoapi-globalmemorystatusex"/></remarks>
    /// <param name="lpBuffer"></param>
    /// <returns></returns>
    [LibraryImport("Kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GlobalMemoryStatusEx(ref MemoryStatusExE lpBuffer);
}

/// <summary>
/// 包含有关物理内存和虚拟内存（包括扩展内存）的当前状态的信息。该 GlobalMemoryStatusEx在这个构造函数存储信息。
/// <see ref="https://docs.microsoft.com/en-us/windows/win32/api/sysinfoapi/ns-sysinfoapi-memorystatusex"/>
/// </summary>
internal struct MemoryStatusExE
{
    /// <summary>
    /// 结构的大小，以字节为单位，必须在调用 GlobalMemoryStatusEx 之前设置此成员，可以用 Init 方法提前处理
    /// </summary>
    /// <remarks>应当使用本对象提供的 Init ，而不是使用构造函数！</remarks>
    public uint dwLength;

    /// <summary>
    /// 一个介于 0 和 100 之间的数字，用于指定正在使用的物理内存的大致百分比（0 表示没有内存使用，100 表示内存已满）。
    /// </summary>
    public uint dwMemoryLoad;

    /// <summary>
    /// 实际物理内存量，以字节为单位
    /// </summary>
    public ulong ullTotalPhys;

    /// <summary>
    /// 当前可用的物理内存量，以字节为单位。这是可以立即重用而无需先将其内容写入磁盘的物理内存量。它是备用列表、空闲列表和零列表的大小之和
    /// </summary>
    public ulong ullAvailPhys;

    /// <summary>
    /// 系统或当前进程的当前已提交内存限制，以字节为单位，以较小者为准。要获得系统范围的承诺内存限制，请调用GetPerformanceInfo
    /// </summary>
    public ulong ullTotalPageFile;

    /// <summary>
    /// 当前进程可以提交的最大内存量，以字节为单位。该值等于或小于系统范围的可用提交值。要计算整个系统的可承诺值，调用GetPerformanceInfo核减价值CommitTotal从价值CommitLimit
    /// </summary>

    public ulong ullAvailPageFile;

    /// <summary>
    /// 调用进程的虚拟地址空间的用户模式部分的大小，以字节为单位。该值取决于进程类型、处理器类型和操作系统的配置。例如，对于 x86 处理器上的大多数 32 位进程，此值约为 2 GB，对于在启用4 GB 调整的系统上运行的具有大地址感知能力的 32 位进程约为 3 GB 。
    /// </summary>

    public ulong ullTotalVirtual;

    /// <summary>
    /// 当前在调用进程的虚拟地址空间的用户模式部分中未保留和未提交的内存量，以字节为单位
    /// </summary>
    public ulong ullAvailVirtual;


    /// <summary>
    /// 预订的。该值始终为 0
    /// </summary>
    public ulong ullAvailExtendedVirtual;

    /// <summary>
    /// 
    /// </summary>
    public void Init()
    {
        dwLength = checked((uint)Marshal.SizeOf(typeof(MemoryStatusExE)));
    }
}
