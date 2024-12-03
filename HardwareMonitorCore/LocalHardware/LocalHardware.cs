namespace HardwareMonitorCore.LocalHardware
{
    using System;
    using System.Management;

    public class LocalHardware
    {

        public static ulong GetAvailablePhysicalMemory()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem"))
            {
                foreach (var obj in searcher.Get())
                {
                    return (ulong)obj["FreePhysicalMemory"] * 1024; //Convert from KB to Bytes
                }
            }
            return 0;
        }
        public static ulong GetTotalPhysicalMemory()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
            {
                foreach (var obj in searcher.Get())
                {
                    return (ulong)obj["TotalPhysicalMemory"];
                }
            }
            return 0;
        }
    }
}
