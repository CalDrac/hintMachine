﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HintMachine
{
    public abstract class IGameConnectorProcess32Bit : IGameConnector
    {
        const int PROCESS_WM_READ = 0x0010;

        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern Microsoft.Win32.SafeHandles.SafeAccessTokenHandle OpenThread(
           ThreadAccess dwDesiredAccess,
           bool bInheritHandle,
           uint dwThreadId
           );

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess,
          Int64 lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("ntdll.dll")]
        public static extern int NtQueryInformationThread(IntPtr processHandle, int threadInformationClass, IntPtr threadInformation, uint threadInformationLength, IntPtr returnLength);

        // ----------------------------------------------------------------------------------

        public string processName;
        public string moduleName;
        public Process process = null;
        public ProcessModule module = null;
        public IntPtr processHandle = IntPtr.Zero;

        public IGameConnectorProcess32Bit(string processName, string moduleName = "")
        {
            this.processName = processName;
            this.moduleName = moduleName;
        }

        public override bool Connect()
        {
            TokenManipulator.AddPrivilege("SeDebugPrivilege");
            TokenManipulator.AddPrivilege("SeSystemEnvironmentPrivilege");
            Process.EnterDebugMode();

            Process[] processes = Process.GetProcessesByName(processName);
            if(processes.Length == 0)
                return false;
            process = processes[0];

            if(moduleName == "")
            {
                module = process.MainModule;
            }
            else
            {
                foreach (ProcessModule m in process.Modules)
                {
                    if (m.FileName.Equals(moduleName))
                    {
                        module = m;
                        break;
                    }
                }
            }

            processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);
            
            return process != null && module != null && processHandle != IntPtr.Zero;
        }

        public override void Disconnect()
        {
            // TODO
        }

        protected long ResolvePointerPath(long baseAddress, int[] offsets)
        {
            long addr = baseAddress;
            foreach (int offset in offsets)
            {
                addr = ReadInt64(addr);
                if (addr == 0)
                    break;

                addr += offset;
            }
            return addr;
        }

        protected byte ReadUint8(long address)
        {
            if (processHandle == IntPtr.Zero)
                return 0;

            int bytesRead = 0;
            byte[] buffer = new byte[sizeof(byte)];
            ReadProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesRead);
            return buffer[0];
        }

        protected ushort ReadUint16(long address)
        {
            if (processHandle == IntPtr.Zero)
                return 0;

            int bytesRead = 0;
            byte[] buffer = new byte[sizeof(ushort)];
            ReadProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesRead);
            return BitConverter.ToUInt16(buffer, 0);
        }

        protected uint ReadUint32(long address)
        {
            if (processHandle == IntPtr.Zero)
                return 0;

            int bytesRead = 0;
            byte[] buffer = new byte[sizeof(uint)];
            ReadProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesRead);
            return BitConverter.ToUInt32(buffer, 0);
        }

        protected long ReadInt64(long address)
        {
            if (processHandle == IntPtr.Zero)
                return 0;

            int bytesRead = 0;
            byte[] buffer = new byte[sizeof(long)];
            ReadProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesRead);
            return BitConverter.ToInt64(buffer, 0);
        }
    }
}
