using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;

namespace HardwareMonitor.Controllers
{
    [Route("api/AMDGPU/{Action}")]
    public class AMDGPUController : ControllerBase
    {
        private static string gpuName = "";
        private static int gpuMaxPower = 100;
        public AMDGPUController(IConfiguration config)
        {
            gpuName = config["GPU:Name"] ?? "";
            gpuMaxPower = config.GetValue<int>("GPU:MaxPower");
        }
        private static readonly object _adlxHelpInitLock = new object();
        protected static ADLXHelper? _adlxHelper = null;
        protected internal static ADLXHelper adlxHelper
        {
            get
            {
                lock (_adlxHelpInitLock)
                {
                    if (_adlxHelper == null)
                    {
                        _adlxHelper = new ADLXHelper();
                        ADLX_RESULT res = _adlxHelper.Initialize();

                        if (res != ADLX_RESULT.ADLX_OK)
                        {
                            _adlxHelper = null;
                            throw new InvalidOperationException("Could not properly initialise the ADLX helper.");
                        }
                    }
                }
                return _adlxHelper;
            }
        }
        protected internal static bool ADLXHelperInitialized => _adlxHelper != null;

        private static readonly object _adlxGetGPULock = new object();
        protected static IADLXGPU? _targetGPU = null;
        protected static IADLXGPU? targetGPU
        {
            get
            {
                lock (_adlxGetGPULock)
                {
                    if (_targetGPU == null)
                    {
                        IADLXSystem sys = adlxHelper.GetSystemServices();
                        if (sys != null)
                        {
                            SWIGTYPE_p_p_adlx__IADLXGPUList ppGPUS = ADLX.new_gpuListP_Ptr();
                            ADLX_RESULT res = sys.GetGPUs(ppGPUS);
                            if (res == ADLX_RESULT.ADLX_OK)
                            {
                                IADLXGPUList gpuList = ADLX.gpuListP_Ptr_value(ppGPUS);

                                for (uint iGPU = gpuList.Begin(); iGPU != gpuList.Size(); iGPU++)
                                {
                                    SWIGTYPE_p_p_adlx__IADLXGPU ppGPU = ADLX.new_gpuP_Ptr();
                                    res = gpuList.At(iGPU, ppGPU);
                                    IADLXGPU sGPU = ADLX.gpuP_Ptr_value(ppGPU);

                                    SWIGTYPE_p_p_char ppGPUName = ADLX.new_charP_Ptr();
                                    sGPU.Name(ppGPUName);
                                    string name = ADLX.charP_Ptr_value(ppGPUName);

                                    if (sGPU != null && name == gpuName)
                                    {
                                        _targetGPU = sGPU;
                                        break;
                                    }
                                }

                            }
                        }
                    }
                }
                return _targetGPU;
            }
        }

        [HttpGet]
        public AMDGPUInfo GetGPUValues()
        {
            try
            {
                AMDGPUInfo gpuInfo = new AMDGPUInfo();

                IADLXSystem sys = adlxHelper.GetSystemServices();

                if (sys != null && targetGPU != null)
                {
                    SWIGTYPE_p_p_adlx__IADLXPerformanceMonitoringServices ppPerfMonServices = ADLX.new_performanceMonSerP_Ptr();
                    ADLX_RESULT res = sys.GetPerformanceMonitoringServices(ppPerfMonServices);

                    if (res == ADLX_RESULT.ADLX_OK)
                    {
                        SWIGTYPE_p_p_adlx__IADLXGPUMetrics ppGPUMetrics = ADLX.new_gpuMetrics_Ptr();
                        IADLXPerformanceMonitoringServices perfSerivce = ADLX.performanceMonSerP_Ptr_value(ppPerfMonServices);
                        res = perfSerivce.GetCurrentGPUMetrics(targetGPU, ppGPUMetrics);

                        SWIGTYPE_p_double pdVal = ADLX.new_doubleP();
                        SWIGTYPE_p_int piVal = ADLX.new_intP();
                        if (res == ADLX_RESULT.ADLX_OK)
                        {
                            IADLXGPUMetrics metrics = ADLX.gpuMetrics_Ptr_value(ppGPUMetrics);

                            metrics.GPUTemperature(pdVal);
                            gpuInfo.temperatureCore = ADLX.doubleP_value(pdVal);

                            metrics.GPUHotspotTemperature(pdVal);
                            gpuInfo.temperatureHotSpot = ADLX.doubleP_value(pdVal);

                            metrics.GPUTotalBoardPower(pdVal);
                            gpuInfo.powerTotal = ADLX.doubleP_value(pdVal);

                            metrics.GPUFanSpeed(piVal);
                            gpuInfo.fan = ADLX.intP_value(piVal);
                        }
                        SWIGTYPE_p_p_adlx__IADLXFPS ppFPS = ADLX.new_fps_Ptr();
                        res = perfSerivce.GetCurrentFPS(ppFPS);
                        if (res == ADLX_RESULT.ADLX_OK)
                        {
                            IADLXFPS fps = ADLX.fps_Ptr_value(ppFPS);

                            res = fps.FPS(piVal);
                            if (res == ADLX_RESULT.ADLX_OK)
                            {
                                gpuInfo.fps = ADLX.intP_value(piVal);
                            }
                            else
                            {
                                gpuInfo.fps = null;
                            }
                        }

                        gpuInfo.powerMax = gpuMaxPower;
                    }
                }
                return gpuInfo;
            }
            catch (Exception)
            {
                // Clear the helper and gpu classes to force reinitialization
                if (_adlxHelper != null)
                {
                    _adlxHelper.Terminate();
                    _adlxHelper.Dispose();
                    _adlxHelper = null;
                }
                _targetGPU = null;
                throw;
            }
        }
    }

    public class AMDGPUInfo
    {
        public double? temperatureCore { get; set; }
        public double? temperatureHotSpot { get; set; }
        public double? powerTotal { get; set; }
        public double? powerMax { get; set; }
        public double? fan { get; set; }
        public int? fps { get; set; }
    }
}