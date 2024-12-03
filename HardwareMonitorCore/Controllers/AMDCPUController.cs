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
    [Route("api/AMDCPU/{Action}")]
    public class AMDCPUController : ControllerBase
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
        private static CPUID[][][]? statThreads;
        private static Collection<GenericCPU>? _amdCPUHandlers = null;
        private static Collection<GenericCPU> AMDCPUHandlers
        {
            get
            {
                if (_amdCPUHandlers == null || _amdCPUHandlers.Count() == 0)
                {
                    _amdCPUHandlers = new Collection<GenericCPU>();
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
                            case Vendor.AMD:
                                switch (threads[0].Family)
                                {
                                    case 0x0F:
                                        _amdCPUHandlers.Add(new AMD0FCPU(index, coreThreads));
                                        break;
                                    case 0x10:
                                    case 0x11:
                                    case 0x12:
                                    case 0x14:
                                    case 0x15:
                                    case 0x16:
                                        _amdCPUHandlers.Add(new AMD10CPU(index, coreThreads));
                                        break;
                                    case 0x17:
                                    case 0x19:
                                        _amdCPUHandlers.Add(new AMD17CPU(index, coreThreads));
                                        break;
                                    default:
                                        _amdCPUHandlers.Add(new GenericCPU(index, coreThreads));
                                        break;
                                }                               
                                break;
                            default:
                                throw new NotSupportedException("Only Intel Processors are currently supported");
                        }

                        index++;
                    }
                }
                return _amdCPUHandlers;
            }
        }
        [HttpGet]
        public AMDCpuInfo[]? GetCPUValues()
        {
            try
            {
                if (!Ring0.IsOpen) ResetAPI();

                AMDCpuInfo[] cpuInfos = new AMDCpuInfo[AMDCPUHandlers.Count()];

                for (int i = 0; i < cpuInfos.Length; i++)
                {
                    AMDCPUHandlers[i].Update();

                    if (AMDCPUHandlers[i] is AMD0FCPU)
                    {
                        Sensor temp = new Sensor() { Value = ((AMD0FCPU)AMDCPUHandlers[i]).coreTemperatures.Average(t => t.Value) };
                        cpuInfos[i] = new AMDCpuInfo()
                        {
                            coreClocks = ((AMD0FCPU)AMDCPUHandlers[i]).coreClocks,
                            busClock = ((AMD0FCPU)AMDCPUHandlers[i]).busClock,
                            coreTemp = temp,
                            packageTemp = temp,
                            powerInfo = new Sensor() { Value = 0 }
                        };
                    }
                    else if (AMDCPUHandlers[i] is AMD10CPU)
                    {
                        cpuInfos[i] = new AMDCpuInfo()
                        {
                            coreClocks = ((AMD10CPU)AMDCPUHandlers[i]).coreClocks,
                            busClock = ((AMD10CPU)AMDCPUHandlers[i]).busClock,
                            coreTemp = ((AMD10CPU)AMDCPUHandlers[i]).coreTemperature,
                            packageTemp = ((AMD10CPU)AMDCPUHandlers[i]).coreTemperature,
                            powerInfo = new Sensor() { Value = 0 }
                        };
                    }
                    else if (AMDCPUHandlers[i] is AMD17CPU)
                    {
                        Sensor[] coreClocks = new Sensor[((AMD17CPU)AMDCPUHandlers[i]).cores.Length];
                        for (int iC = 0; iC < coreClocks.Length; iC++)
                        {
                            coreClocks[iC] = ((AMD17CPU)AMDCPUHandlers[i]).cores[iC].clockSensor;
                        }
                        cpuInfos[i] = new AMDCpuInfo()
                        {
                            coreClocks = coreClocks,
                            busClock = ((AMD17CPU)AMDCPUHandlers[i]).busClock,
                            coreTemp = ((AMD17CPU)AMDCPUHandlers[i]).coreTemperature,
                            packageTemp = ((AMD17CPU)AMDCPUHandlers[i]).coreTemperature,
                            powerInfo = ((AMD17CPU)AMDCPUHandlers[i]).packagePowerSensor
                        };
                    }
                    //Check PowerInfo, if it's all null, then trigger a reset of the API
                    if (cpuInfos[i].packageTemp == null)
                    {
                        ResetAPI();
                        // Still return the result, the rest seems to be fine on restart, just the power doesn't come through.
                    }
                }

                return cpuInfos;
            }
            catch (NullReferenceException)
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

    public class AMDCpuInfo
    {
        public Sensor? coreTemp { get; set; }
        public Sensor? packageTemp { get; set; }
        public Sensor[]? coreClocks { get; set; }
        public Sensor? busClock { get; set; }
        public Sensor? powerInfo { get; set; }
    }

}