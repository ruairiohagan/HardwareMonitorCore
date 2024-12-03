using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HardwareMonitor.OpenHardware.HardwareHelper;
using Microsoft.AspNetCore.Mvc;
using OpenHardwareMonitor.Hardware;
using OpenHardwareMonitor.Hardware.CPU;

namespace HardwareMonitor.Controllers
{
    [Route("api/IntelCPU/{Action}")]
    public class IntelCPUController : ControllerBase
    {
        private static CPUID[][] GetProcessorThreads()
        {

            List<CPUID> threads = new List<CPUID>();
            for (int i = 0; i < ThreadAffinity.ProcessorGroupCount; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    try
                    {
                        if (!ThreadAffinity.IsValid(GroupAffinity.Single((ushort)i, j)))
                            continue;
                        var cpuid = CPUID.Get(i, j);
                        if (cpuid != null)
                            threads.Add(cpuid);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                    }
                }
            }

            SortedDictionary<uint, List<CPUID>> processors =
              new SortedDictionary<uint, List<CPUID>>();
            foreach (CPUID thread in threads)
            {
                List<CPUID>? list;
                processors.TryGetValue(thread.ProcessorId, out list);
                if (list == null)
                {
                    list = new List<CPUID>();
                    processors.Add(thread.ProcessorId, list);
                }
                list.Add(thread);
            }

            CPUID[][] processorThreads = new CPUID[processors.Count][];
            int index = 0;
            foreach (List<CPUID> list in processors.Values)
            {
                processorThreads[index] = list.ToArray();
                index++;
            }
            return processorThreads;
        }
        private static CPUID[][] GroupThreadsByCore(IEnumerable<CPUID> threads)
        {

            SortedDictionary<uint, List<CPUID>> cores =
              new SortedDictionary<uint, List<CPUID>>();
            foreach (CPUID thread in threads)
            {
                List<CPUID>? coreList;
                cores.TryGetValue(thread.CoreId, out coreList);
                if (coreList == null)
                {
                    coreList = new List<CPUID>();
                    cores.Add(thread.CoreId, coreList);
                }
                coreList.Add(thread);
            }

            CPUID[][] coreThreads = new CPUID[cores.Count][];
            int index = 0;
            foreach (List<CPUID> list in cores.Values)
            {
                coreThreads[index] = list.ToArray();
                index++;
            }
            return coreThreads;
        }
        private static CPUID[][][] statThreads = [];
        private static Collection<IntelCPU>? _intelCPUHandlers = null;
        private static Collection<IntelCPU> IntelCPUHandlers
        {
            get
            {
                if (_intelCPUHandlers == null)
                {
                    _intelCPUHandlers = new Collection<IntelCPU>();
                    CPUID[][] processorThreads = GetProcessorThreads();
                    statThreads = new CPUID[processorThreads.Length][][];

                    int index = 0;
                    foreach (CPUID[] threads in processorThreads)
                    {
                        if (threads.Length == 0)
                            continue;

                        CPUID[][] coreThreads = GroupThreadsByCore(threads);

                        statThreads[index] = coreThreads;

                        switch (threads[0].Vendor)
                        {
                            case Vendor.Intel:
                                _intelCPUHandlers.Add(new IntelCPU(index, coreThreads));
                                break;
                            default:
                                throw new NotSupportedException("Only Intel Processors are currently supported");
                        }

                        index++;
                    }
                }
                return _intelCPUHandlers;
            }
        }
        [HttpGet]
        public CpuInfo[]? GetCPUValues()
        {
            try
            {
                CpuInfo[] cpuInfos = new CpuInfo[IntelCPUHandlers.Count()];

                for (int i = 0; i < cpuInfos.Length; i++)
                {
                    IntelCPUHandlers[i].Update();

                    cpuInfos[i] = new CpuInfo()
                    {
                        CoreClocks = IntelCPUHandlers[i].coreClocks,
                        BusClock = IntelCPUHandlers[i].busClock,
                        CoreTemps = IntelCPUHandlers[i].coreTemperatures,
                        PackageTemp = IntelCPUHandlers[i].packageTemperature,
                        PowerInfo = IntelCPUHandlers[i].powerSensors
                    };
                    //Check PowerInfo, if it's all null, then trigger a reset of the API
                    if (cpuInfos[i]?.PowerInfo == null || (cpuInfos[i].PowerInfo?.All(p => p.Value == null) ?? false))
                    {
                        ResetAPI();
                        // Still return the result, the rest seems to be fine on restart, just the power doesn't come through.
                    }
                }

                return cpuInfos;
            }
            catch(NullReferenceException) 
            {
                ResetAPI();

                return null;
            }
        }
        protected void ResetAPI()
        {
            Ring0.Close();
            Opcode.Close();

            Ring0.Open();
            Opcode.Open();
        }
    }

    public class CpuInfo
    {
        public TempData[]? CoreTemps { get; set; }
        public TempData? PackageTemp { get; set; }
        public Sensor[]? CoreClocks { get; set; }
        public Sensor? BusClock { get; set; }
        public PowerData[]? PowerInfo { get; set; }
    }
}