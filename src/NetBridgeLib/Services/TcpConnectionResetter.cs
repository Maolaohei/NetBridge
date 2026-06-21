using System.Net;
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
    {
        int resetCount = 0;
        int bufferSize = 0;

        uint result = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, false, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);

        if (result != 122)
        {
            return 0;
        }

        IntPtr tcpTablePtr = Marshal.AllocHGlobal(bufferSize);

        try
        {
            result = GetExtendedTcpTable(tcpTablePtr, ref bufferSize, false, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);

            if (result != 0)
            {
                return 0;
            }

            int rowCount = Marshal.ReadInt32(tcpTablePtr);
            IntPtr rowPtr = tcpTablePtr + 4;

            int rowSize = Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();

            for (int i = 0; i < rowCount; i++)
            {
                var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);

                if (row.dwOwningPid == pid && row.dwState != MIB_TCP_STATE_DELETE_TCB)
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

    public static int ResetConnectionsForProcesses(IEnumerable<uint> pids)
    {
        var pidSet = new HashSet<uint>(pids);
        int totalReset = 0;

        int bufferSize = 0;

        uint result = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, false, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);

        if (result != 122)
        {
            return 0;
        }

        IntPtr tcpTablePtr = Marshal.AllocHGlobal(bufferSize);

        try
        {
            result = GetExtendedTcpTable(tcpTablePtr, ref bufferSize, false, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);

            if (result != 0)
            {
                return 0;
            }

            int rowCount = Marshal.ReadInt32(tcpTablePtr);
            IntPtr rowPtr = tcpTablePtr + 4;

            int rowSize = Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();

            for (int i = 0; i < rowCount; i++)
            {
                var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);

                if (pidSet.Contains(row.dwOwningPid) && row.dwState != MIB_TCP_STATE_DELETE_TCB)
                {
                    var deleteRow = row;
                    deleteRow.dwState = MIB_TCP_STATE_DELETE_TCB;
                    SetTcpEntry(ref deleteRow);
                    totalReset++;
                }

                rowPtr += rowSize;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(tcpTablePtr);
        }

        return totalReset;
    }

    public static int ResetAllConnections()
    {
        int resetCount = 0;
        int bufferSize = 0;

        uint result = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, false, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);

        if (result != 122)
        {
            return 0;
        }

        IntPtr tcpTablePtr = Marshal.AllocHGlobal(bufferSize);

        try
        {
            result = GetExtendedTcpTable(tcpTablePtr, ref bufferSize, false, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);

            if (result != 0)
            {
                return 0;
            }

            int rowCount = Marshal.ReadInt32(tcpTablePtr);
            IntPtr rowPtr = tcpTablePtr + 4;

            int rowSize = Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();

            for (int i = 0; i < rowCount; i++)
            {
                var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);

                if (row.dwState != MIB_TCP_STATE_DELETE_TCB)
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
            lock (Lock)
            {
                TrackedPids.Add(pid);
            }
        }

        public static HashSet<uint> GetAndClear()
        {
            lock (Lock)
            {
                var copy = new HashSet<uint>(TrackedPids);
                TrackedPids.Clear();
                return copy;
            }
        }
    }

    public static void TrackConnection(uint pid)
    {
        ProcessTracker.Track(pid);
    }

    public static int ResetTrackedConnections()
    {
        var pids = ProcessTracker.GetAndClear();
        return ResetConnectionsForProcesses(pids);
    }
}
