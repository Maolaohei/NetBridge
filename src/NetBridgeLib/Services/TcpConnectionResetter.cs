using System.Runtime.InteropServices;

namespace NetBridgeLib.Services;

public static class TcpConnectionResetter
{
    private const int AF_INET = 2;
    private const int MIB_TCP_STATE_DELETE_TCB = 12;

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_TCPROW_OWNER_PID
    {
        public uint dwState;
        public uint dwLocalAddr;
        public uint dwLocalPort;
        public uint dwRemoteAddr;
        public uint dwRemotePort;
        public uint dwOwningPid;
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int dwOutBufLen,
        bool sort,
        int ipVersion,
        TCP_TABLE_CLASS tableClass,
        uint reserved);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint SetTcpEntry(ref MIB_TCPROW_OWNER_PID pTcprow);

    private enum TCP_TABLE_CLASS
    {
        TCP_TABLE_OWNER_PID_ALL = 5
    }

    public static int ResetConnectionsForProcess(uint pid)
        => ResetConnectionsForProcesses(new[] { pid });

    public static int ResetConnectionsForProcesses(IEnumerable<uint> pids)
    {
        var pidSet = new HashSet<uint>(pids);
        if (pidSet.Count == 0) return 0;
        return ResetIpv4ForPids(pidSet);
    }

    public static int ResetAllConnections()
        => ResetIpv4ForPids(null);

    private static int ResetIpv4ForPids(HashSet<uint>? pidSet)
    {
        int resetCount = 0;
        int bufferSize = 0;

        uint result = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, false, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
        if (result != 122 || bufferSize <= 0) return 0;

        IntPtr tcpTablePtr = Marshal.AllocHGlobal(bufferSize);
        try
        {
            result = GetExtendedTcpTable(tcpTablePtr, ref bufferSize, false, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
            if (result != 0) return 0;

            int rowCount = Marshal.ReadInt32(tcpTablePtr);
            IntPtr rowPtr = tcpTablePtr + 4;
            int rowSize = Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();

            for (int i = 0; i < rowCount; i++)
            {
                var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);
                var match = pidSet == null || pidSet.Contains(row.dwOwningPid);
                if (match && row.dwState != MIB_TCP_STATE_DELETE_TCB)
                {
                    var deleteRow = row;
                    deleteRow.dwState = MIB_TCP_STATE_DELETE_TCB;
                    SetTcpEntry(ref deleteRow);
                    resetCount++;
                }
                rowPtr += rowSize;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(tcpTablePtr);
        }

        return resetCount;
    }

    private static class ProcessTracker
    {
        private static readonly HashSet<uint> TrackedPids = new();
        private static readonly object Lock = new();

        public static void Track(uint pid)
        {
            if (pid == 0) return;
            lock (Lock)
            {
                TrackedPids.Add(pid);
            }
        }

        public static HashSet<uint> Snapshot()
        {
            lock (Lock)
            {
                return new HashSet<uint>(TrackedPids);
            }
        }
    }

    public static void TrackConnection(uint pid) => ProcessTracker.Track(pid);

    /// <summary>
    /// Reset TCP connections for all PIDs observed via NetBridge connection callbacks.
    /// Keeps the tracker so subsequent system-proxy toggles still reset correctly.
    /// </summary>
    public static int ResetTrackedConnections()
    {
        var pids = ProcessTracker.Snapshot();
        return ResetConnectionsForProcesses(pids);
    }
}
