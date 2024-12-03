using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using HardwareMonitorCore.LocalHardware;

namespace HardwareMonitor.Controllers
{
    [Route("api/Perf/{Action}")]
    public class PerfController : ControllerBase
    {
        protected static string GetPerfIndexer(string category, string counter, string instance)
        {
            return $"{category}|{counter}|{instance}";
        }
        protected static ConcurrentDictionary<string, PerformanceCounter?> perfCounters = new ConcurrentDictionary<string, PerformanceCounter?>();

        [HttpGet]
        public float GetPerfCounterValue(string category, string counter, string instance)
        {
            string indexer = GetPerfIndexer(category, counter, instance);
            PerformanceCounter? pc = null;
            if (perfCounters.TryAdd(indexer, null))
            {
                pc = new PerformanceCounter(category, counter, instance);
                perfCounters[indexer] = pc;
                pc.NextValue();
            }
            else
            {
                pc = perfCounters[indexer];
            }

            return pc?.NextValue() ?? 0;
        }

        [HttpGet]
        public float GetSystemMemoryUsed()
        {
            return LocalHardware.GetTotalPhysicalMemory() - LocalHardware.GetAvailablePhysicalMemory();
        }
        [HttpGet]
        public float GetGPUMemoryUsed()
        {
            PerformanceCounterCategory pc = new PerformanceCounterCategory("GPU Adapter Memory");
            string[] instances = pc.GetInstanceNames();

            float maxMem = 0;
            foreach (string instance in instances)
            {
                float curMem = GetPerfCounterValue(pc.CategoryName, "Dedicated Usage", instance);
                if (curMem > maxMem) maxMem = curMem;
            }
            return maxMem;
        }
        [HttpGet]
        public DiskInfo[] GetDiskData()
        {
            PerformanceCounterCategory pccPD = new PerformanceCounterCategory("PhysicalDisk");
            string[] diskNames = pccPD.GetInstanceNames();

            Collection <DiskInfo> colDI = new Collection<DiskInfo>();
            foreach (DriveInfo di in DriveInfo.GetDrives().OrderBy(d => d.Name))
            {
                if (di.Name.Contains(":\\"))
                {
                    string? instanceName = diskNames.Where(d => d.Contains(di.Name.Replace("\\", ""))).FirstOrDefault();
                    if (instanceName != null)
                    {
                        colDI.Add(new DiskInfo()
                        {
                            drive = di.Name.Replace("\\", ""),
                            util = GetPerfCounterValue("PhysicalDisk", "% Disk Time", instanceName),
                            totalBytesPerSec = GetPerfCounterValue("PhysicalDisk", "Disk Bytes/sec", instanceName),
                        });
                    }
                }
            }
            return colDI.ToArray();
        }
    }

    public class DiskInfo
    {
        public string? drive { get; set; }
        public float util { get; set; }
        public float totalBytesPerSec {get;set; }
    }
}