using HardwareMonitor.OpenHardware.HardwareHelper;
using Microsoft.AspNetCore.Mvc;
using OpenHardwareMonitor.Hardware.ATI;
using System;
using System.Text;

namespace HardwareMonitor.Controllers
{
    [Route("api/AMDGPU/{Action}")]
    public class AMDGPUController : ControllerBase
    {
        private static ATIGPU? _atiGPUHandler = null;
        private ATIGPU atiGPUHandler
        {
            get 
            {
                if (_atiGPUHandler == null)
                {
                    IntPtr context = IntPtr.Zero;

                    var adlStatus = ADL.ADL_Main_Control_Create(1);
                    var adl2Status = ADL.ADL2_Main_Control_Create(1, out context);

                    var status = ADL.ADL_Graphics_Versions_Get(out var versionInfo);

                    if (adlStatus == ADLStatus.OK)
                    {
                        int numberOfAdapters = 0;
                        ADL.ADL_Adapter_NumberOfAdapters_Get(ref numberOfAdapters);

                        if (numberOfAdapters > 0)
                        {
                            ADLAdapterInfo[] adapterInfo = new ADLAdapterInfo[numberOfAdapters];
                            if (ADL.ADL_Adapter_AdapterInfo_Get(adapterInfo) == ADLStatus.OK)
                                for (int i = 0; i < numberOfAdapters; i++)
                                {
                                    int isActive;
                                    ADL.ADL_Adapter_Active_Get(adapterInfo[i].AdapterIndex,
                                      out isActive);
                                    int adapterID;
                                    ADL.ADL_Adapter_ID_Get(adapterInfo[i].AdapterIndex,
                                      out adapterID);

                                    if (!string.IsNullOrEmpty(adapterInfo[i].UDID) &&
                                      adapterInfo[i].VendorID == ADL.ATI_VENDOR_ID)
                                    {
                                        var nameBuilder = new StringBuilder(adapterInfo[i].AdapterName);
                                        nameBuilder.Replace("(TM)", " ");
                                        for (int j = 0; j < 10; j++) nameBuilder.Replace("  ", " ");
                                        var name = nameBuilder.ToString().Trim();

                                        _atiGPUHandler = new ATIGPU(name,
                                          adapterInfo[i].AdapterIndex,
                                          adapterInfo[i].BusNumber,
                                          adapterInfo[i].DeviceNumber, context);
                                    }
                                }
                        }
                    }
                    if (_atiGPUHandler == null)
                    {
                        throw new InvalidOperationException("Could not properly initialise the ATI graphics card controller.");
                    }
                }
                return _atiGPUHandler; 
            }
        }

        [HttpGet]
        public AMDGPUInfo GetGPUValues()
        {
            atiGPUHandler.Update();

            AMDGPUInfo gpuInfo = new AMDGPUInfo()
            {
                temperatureCore = atiGPUHandler.temperatureCore.Value,
                temperatureMemory = atiGPUHandler.temperatureMemory.Value,
                temperatureVrmCore = atiGPUHandler.temperatureVrmCore.Value,
                temperatureVrmMemory = atiGPUHandler.temperatureVrmMemory.Value,
                temperatureVrmMemory0 = atiGPUHandler.temperatureVrmMemory0.Value,
                temperatureVrmMemory1 = atiGPUHandler.temperatureVrmMemory1.Value,
                temperatureLiquid = atiGPUHandler.temperatureLiquid.Value,
                temperaturePlx = atiGPUHandler.temperaturePlx.Value,
                temperatureHotSpot = atiGPUHandler.temperatureHotSpot.Value,
                temperatureVrmSoc = atiGPUHandler.temperatureVrmSoc.Value,
                powerCore = atiGPUHandler.powerCore.Value,
                powerPpt = atiGPUHandler.powerPpt.Value,
                powerSocket = atiGPUHandler.powerSocket.Value,
                powerTotal = atiGPUHandler.powerTotal.Value,
                powerSoc = atiGPUHandler.powerSoc.Value,
                fan = atiGPUHandler.fan.Value,
                fanPercentage = atiGPUHandler.fanPercentage.Value,
                coreClock = atiGPUHandler.coreClock.Value,
                memoryClock = atiGPUHandler.memoryClock.Value,
                socClock = atiGPUHandler.socClock.Value,
                coreVoltage = atiGPUHandler.coreVoltage.Value,
                memoryVoltage = atiGPUHandler.memoryVoltage.Value,
                socVoltage = atiGPUHandler.socVoltage.Value,
                coreLoad = atiGPUHandler.coreLoad.Value,
                memoryLoad = atiGPUHandler.memoryLoad.Value
            };

            return gpuInfo;
        }
    }

    public class AMDGPUInfo
    {
        public float? temperatureCore { get; set; }
        public float? temperatureMemory { get; set; }
        public float? temperatureVrmCore { get; set; }
        public float? temperatureVrmMemory { get; set; }
        public float? temperatureVrmMemory0 { get; set; }
        public float? temperatureVrmMemory1 { get; set; }
        public float? temperatureLiquid { get; set; }
        public float? temperaturePlx { get; set; }
        public float? temperatureHotSpot { get; set; }
        public float? temperatureVrmSoc { get; set; }
        public float? powerCore { get; set; }
        public float? powerPpt { get; set; }
        public float? powerSocket { get; set; }
        public float? powerTotal { get; set; }
        public float? powerSoc { get; set; }
        public float? fan { get; set; }
        public float? fanPercentage { get; set; }
        public float? coreClock { get; set; }
        public float? memoryClock { get; set; }
        public float? socClock { get; set; }
        public float? coreVoltage { get; set; }
        public float? memoryVoltage { get; set; }
        public float? socVoltage { get; set; }
        public float? coreLoad { get; set; }
        public float? memoryLoad { get; set; }
    }
}