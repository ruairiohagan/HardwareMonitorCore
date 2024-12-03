/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2020 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using HardwareMonitor.OpenHardware.HardwareHelper;
using System;
using System.Globalization;
using System.Text;

namespace OpenHardwareMonitor.Hardware.ATI
{
    internal sealed class ATIGPU : Hardware
    {

        private readonly int adapterIndex;
        private readonly int busNumber;
        private readonly int deviceNumber;
        public readonly TempData temperatureCore;
        public readonly TempData temperatureMemory;
        public readonly TempData temperatureVrmCore;
        public readonly TempData temperatureVrmMemory;
        public readonly TempData temperatureVrmMemory0;
        public readonly TempData temperatureVrmMemory1;
        public readonly TempData temperatureLiquid;
        public readonly TempData temperaturePlx;
        public readonly TempData temperatureHotSpot;
        public readonly TempData temperatureVrmSoc;
        public readonly PowerData powerCore;
        public readonly PowerData powerPpt;
        public readonly PowerData powerSocket;
        public readonly PowerData powerTotal;
        public readonly PowerData powerSoc;
        public readonly FanData fan;
        public readonly FanData fanPercentage;
        public readonly Sensor coreClock;
        public readonly Sensor memoryClock;
        public readonly Sensor socClock;
        public readonly VoltageData coreVoltage;
        public readonly VoltageData memoryVoltage;
        public readonly VoltageData socVoltage;
        public readonly LoadData coreLoad;
        public readonly LoadData memoryLoad;

        private IntPtr context;
        private readonly int overdriveVersion;

        public ATIGPU(string name, int adapterIndex, int busNumber,
          int deviceNumber, IntPtr context)
          : base(name, new Identifier("atigpu",
            adapterIndex.ToString(CultureInfo.InvariantCulture)))
        {
            this.adapterIndex = adapterIndex;
            this.busNumber = busNumber;
            this.deviceNumber = deviceNumber;

            this.context = context;

            if (ADL.ADL_Overdrive_Caps(adapterIndex, out _, out _,
              out overdriveVersion) != ADLStatus.OK)
            {
                overdriveVersion = -1;
            }

            this.temperatureCore = new TempData();
            this.temperatureMemory = new TempData();
            this.temperatureVrmCore = new TempData();
            this.temperatureVrmMemory = new TempData();
            this.temperatureVrmMemory0 = new TempData();
            this.temperatureVrmMemory1 = new TempData();
            this.temperatureVrmSoc = new TempData();
            this.temperatureLiquid = new TempData();
            this.temperaturePlx = new TempData();
            this.temperatureHotSpot = new TempData();

            this.powerTotal = new PowerData();
            this.powerCore = new PowerData();
            this.powerPpt = new PowerData();
            this.powerSocket = new PowerData();
            this.powerSoc = new PowerData();

            this.fan = new FanData();
            this.fanPercentage = new FanData();

            this.coreClock = new Sensor();
            this.memoryClock = new Sensor();
            this.socClock = new Sensor();

            this.coreVoltage = new VoltageData();
            this.memoryVoltage = new VoltageData();
            this.socVoltage = new VoltageData();

            this.coreLoad = new LoadData();
            this.memoryLoad = new LoadData();

            ADLFanSpeedInfo afsi = new ADLFanSpeedInfo();
            if (ADL.ADL_Overdrive5_FanSpeedInfo_Get(adapterIndex, 0, ref afsi)
              != ADLStatus.OK)
            {
                afsi.MaxPercent = 100;
                afsi.MinPercent = 0;
            }

            Update();
        }

        public int BusNumber { get { return busNumber; } }

        public int DeviceNumber { get { return deviceNumber; } }


        public override HardwareType HardwareType
        {
            get { return HardwareType.GpuAti; }
        }

        private void GetODNTemperature(ADLODNTemperatureType type,
          TempData sensor)
        {
            if (ADL.ADL2_OverdriveN_Temperature_Get(context, adapterIndex,
              type, out int temperature) == ADLStatus.OK)
            {
                sensor.Value = 0.001f * temperature;
            }
            else
            {
                sensor.Value = null;
            }
        }

        private void GetOD6Power(ADLODNCurrentPowerType type, PowerData sensor)
        {
            if (ADL.ADL2_Overdrive6_CurrentPower_Get(context, adapterIndex, type,
              out int power) == ADLStatus.OK)
            {
                sensor.Value = power * (1.0f / 0xFF);
            }
            else
            {
                sensor.Value = null;
            }

        }

        public override string GetReport()
        {
            var r = new StringBuilder();

            r.AppendLine("AMD GPU");
            r.AppendLine();

            r.Append("AdapterIndex: ");
            r.AppendLine(adapterIndex.ToString(CultureInfo.InvariantCulture));
            r.AppendLine();

            r.AppendLine("Overdrive Caps");
            r.AppendLine();
            try
            {
                var status = ADL.ADL_Overdrive_Caps(adapterIndex,
                  out int supported, out int enabled, out int version);
                r.Append(" Status: ");
                r.AppendLine(status.ToString());
                r.Append(" Supported: ");
                r.AppendLine(supported.ToString(CultureInfo.InvariantCulture));
                r.Append(" Enabled: ");
                r.AppendLine(enabled.ToString(CultureInfo.InvariantCulture));
                r.Append(" Version: ");
                r.AppendLine(version.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception e)
            {
                r.AppendLine(" Status: " + e.Message);
            }
            r.AppendLine();

            r.AppendLine("Overdrive5 Parameters");
            r.AppendLine();
            try
            {
                var status = ADL.ADL_Overdrive5_ODParameters_Get(
                  adapterIndex, out var p);
                r.Append(" Status: ");
                r.AppendLine(status.ToString());
                r.AppendFormat(" NumberOfPerformanceLevels: {0}{1}",
                  p.NumberOfPerformanceLevels, Environment.NewLine);
                r.AppendFormat(" ActivityReportingSupported: {0}{1}",
                  p.ActivityReportingSupported, Environment.NewLine);
                r.AppendFormat(" DiscretePerformanceLevels: {0}{1}",
                  p.DiscretePerformanceLevels, Environment.NewLine);
                r.AppendFormat(" EngineClock.Min: {0}{1}",
                  p.EngineClock.Min, Environment.NewLine);
                r.AppendFormat(" EngineClock.Max: {0}{1}",
                  p.EngineClock.Max, Environment.NewLine);
                r.AppendFormat(" EngineClock.Step: {0}{1}",
                  p.EngineClock.Step, Environment.NewLine);
                r.AppendFormat(" MemoryClock.Min: {0}{1}",
                  p.MemoryClock.Min, Environment.NewLine);
                r.AppendFormat(" MemoryClock.Max: {0}{1}",
                  p.MemoryClock.Max, Environment.NewLine);
                r.AppendFormat(" MemoryClock.Step: {0}{1}",
                  p.MemoryClock.Step, Environment.NewLine);
                r.AppendFormat(" Vddc.Min: {0}{1}",
                  p.Vddc.Min, Environment.NewLine);
                r.AppendFormat(" Vddc.Max: {0}{1}",
                  p.Vddc.Max, Environment.NewLine);
                r.AppendFormat(" Vddc.Step: {0}{1}",
                  p.Vddc.Step, Environment.NewLine);
            }
            catch (Exception e)
            {
                r.AppendLine(" Status: " + e.Message);
            }
            r.AppendLine();

            r.AppendLine("Overdrive5 Temperature");
            r.AppendLine();
            try
            {
                var adlt = new ADLTemperature();
                var status = ADL.ADL_Overdrive5_Temperature_Get(adapterIndex, 0,
                  ref adlt);
                r.Append(" Status: ");
                r.AppendLine(status.ToString());
                r.AppendFormat(" Value: {0}{1}",
                  0.001f * adlt.Temperature, Environment.NewLine);
            }
            catch (Exception e)
            {
                r.AppendLine(" Status: " + e.Message);
            }
            r.AppendLine();

            r.AppendLine("Overdrive5 FanSpeed");
            r.AppendLine();
            try
            {
                var adlf = new ADLFanSpeedValue();
                adlf.SpeedType = ADL.ADL_DL_FANCTRL_SPEED_TYPE_RPM;
                var status = ADL.ADL_Overdrive5_FanSpeed_Get(adapterIndex, 0, ref adlf);
                r.Append(" Status RPM: ");
                r.AppendLine(status.ToString());
                r.AppendFormat(" Value RPM: {0}{1}",
                  adlf.FanSpeed, Environment.NewLine);
                adlf.SpeedType = ADL.ADL_DL_FANCTRL_SPEED_TYPE_PERCENT;
                status = ADL.ADL_Overdrive5_FanSpeed_Get(adapterIndex, 0, ref adlf);
                r.Append(" Status Percent: ");
                r.AppendLine(status.ToString());
                r.AppendFormat(" Value Percent: {0}{1}",
                  adlf.FanSpeed, Environment.NewLine);
            }
            catch (Exception e)
            {
                r.AppendLine(" Status: " + e.Message);
            }
            r.AppendLine();

            r.AppendLine("Overdrive5 CurrentActivity");
            r.AppendLine();
            try
            {
                var adlp = new ADLPMActivity();
                var status = ADL.ADL_Overdrive5_CurrentActivity_Get(adapterIndex,
                  ref adlp);
                r.Append(" Status: ");
                r.AppendLine(status.ToString());
                r.AppendFormat(" EngineClock: {0}{1}",
                  0.01f * adlp.EngineClock, Environment.NewLine);
                r.AppendFormat(" MemoryClock: {0}{1}",
                  0.01f * adlp.MemoryClock, Environment.NewLine);
                r.AppendFormat(" Vddc: {0}{1}",
                  0.001f * adlp.Vddc, Environment.NewLine);
                r.AppendFormat(" ActivityPercent: {0}{1}",
                  adlp.ActivityPercent, Environment.NewLine);
                r.AppendFormat(" CurrentPerformanceLevel: {0}{1}",
                  adlp.CurrentPerformanceLevel, Environment.NewLine);
                r.AppendFormat(" CurrentBusSpeed: {0}{1}",
                  adlp.CurrentBusSpeed, Environment.NewLine);
                r.AppendFormat(" CurrentBusLanes: {0}{1}",
                  adlp.CurrentBusLanes, Environment.NewLine);
                r.AppendFormat(" MaximumBusLanes: {0}{1}",
                  adlp.MaximumBusLanes, Environment.NewLine);
            }
            catch (Exception e)
            {
                r.AppendLine(" Status: " + e.Message);
            }
            r.AppendLine();

            if (context != IntPtr.Zero)
            {
                r.AppendLine("Overdrive6 CurrentPower");
                r.AppendLine();
                try
                {
                    for (int i = 0; i < 4; i++)
                    {
                        var pt = ((ADLODNCurrentPowerType)i).ToString();
                        var status = ADL.ADL2_Overdrive6_CurrentPower_Get(
                          context, adapterIndex, (ADLODNCurrentPowerType)i,
                          out int power);
                        if (status == ADLStatus.OK)
                        {
                            r.AppendFormat(" Power[{0}].Value: {1}{2}", pt,
                              power * (1.0f / 0xFF), Environment.NewLine);
                        }
                        else
                        {
                            r.AppendFormat(" Power[{0}].Status: {1}{2}", pt,
                              status.ToString(), Environment.NewLine);
                        }
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    r.AppendLine(" Status: Entry point not found");
                }
                catch (Exception e)
                {
                    r.AppendLine(" Status: " + e.Message);
                }
                r.AppendLine();
            }

            if (context != IntPtr.Zero)
            {
                r.AppendLine("OverdriveN Temperature");
                r.AppendLine();
                try
                {
                    for (int i = 1; i < 8; i++)
                    {
                        var tt = ((ADLODNTemperatureType)i).ToString();
                        var status = ADL.ADL2_OverdriveN_Temperature_Get(
                          context, adapterIndex, (ADLODNTemperatureType)i,
                          out int temperature);
                        if (status == ADLStatus.OK)
                        {
                            r.AppendFormat(" Temperature[{0}].Value: {1}{2}", tt,
                              0.001f * temperature, Environment.NewLine);
                        }
                        else
                        {
                            r.AppendFormat(" Temperature[{0}].Status: {1}{2}", tt,
                              status.ToString(), Environment.NewLine);
                        }
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    r.AppendLine(" Status: Entry point not found");
                }
                catch (Exception e)
                {
                    r.AppendLine(" Status: " + e.Message);
                }
                r.AppendLine();
            }

            if (context != IntPtr.Zero)
            {
                r.AppendLine("OverdriveN Performance Status");
                r.AppendLine();
                try
                {
                    var status = ADL.ADL2_OverdriveN_PerformanceStatus_Get(context,
                      adapterIndex, out var ps);
                    r.Append(" Status: ");
                    r.AppendLine(status.ToString());
                    r.AppendFormat(" CoreClock: {0}{1}",
                      ps.CoreClock, Environment.NewLine);
                    r.AppendFormat(" MemoryClock: {0}{1}",
                      ps.MemoryClock, Environment.NewLine);
                    r.AppendFormat(" DCEFClock: {0}{1}",
                      ps.DCEFClock, Environment.NewLine);
                    r.AppendFormat(" GFXClock: {0}{1}",
                      ps.GFXClock, Environment.NewLine);
                    r.AppendFormat(" UVDClock: {0}{1}",
                      ps.UVDClock, Environment.NewLine);
                    r.AppendFormat(" VCEClock: {0}{1}",
                      ps.VCEClock, Environment.NewLine);
                    r.AppendFormat(" GPUActivityPercent: {0}{1}",
                      ps.GPUActivityPercent, Environment.NewLine);
                    r.AppendFormat(" CurrentCorePerformanceLevel: {0}{1}",
                      ps.CurrentCorePerformanceLevel, Environment.NewLine);
                    r.AppendFormat(" CurrentMemoryPerformanceLevel: {0}{1}",
                      ps.CurrentMemoryPerformanceLevel, Environment.NewLine);
                    r.AppendFormat(" CurrentDCEFPerformanceLevel: {0}{1}",
                      ps.CurrentDCEFPerformanceLevel, Environment.NewLine);
                    r.AppendFormat(" CurrentGFXPerformanceLevel: {0}{1}",
                      ps.CurrentGFXPerformanceLevel, Environment.NewLine);
                    r.AppendFormat(" UVDPerformanceLevel: {0}{1}",
                      ps.UVDPerformanceLevel, Environment.NewLine);
                    r.AppendFormat(" VCEPerformanceLevel: {0}{1}",
                      ps.VCEPerformanceLevel, Environment.NewLine);
                    r.AppendFormat(" CurrentBusSpeed: {0}{1}",
                      ps.CurrentBusSpeed, Environment.NewLine);
                    r.AppendFormat(" CurrentBusLanes: {0}{1}",
                      ps.CurrentBusLanes, Environment.NewLine);
                    r.AppendFormat(" MaximumBusLanes: {0}{1}",
                      ps.MaximumBusLanes, Environment.NewLine);
                    r.AppendFormat(" VDDC: {0}{1}",
                      ps.VDDC, Environment.NewLine);
                    r.AppendFormat(" VDDCI: {0}{1}",
                      ps.VDDCI, Environment.NewLine);
                }
                catch (EntryPointNotFoundException)
                {
                    r.AppendLine(" Status: Entry point not found");
                }
                catch (Exception e)
                {
                    r.AppendLine(" Status: " + e.Message);
                }
                r.AppendLine();
            }

            if (context != IntPtr.Zero)
            {
                r.AppendLine("Performance Metrics");
                r.AppendLine();
                try
                {
                    var status = ADL.ADL2_New_QueryPMLogData_Get(context, adapterIndex,
                      out var data);
                    if (status == ADLStatus.OK)
                    {
                        for (int i = 0; i < data.Sensors.Length; i++)
                        {
                            if (data.Sensors[i].Supported)
                            {
                                var st = ((ADLSensorType)i).ToString();
                                r.AppendFormat(" Sensor[{0}].Value: {1}{2}", st,
                                  data.Sensors[i].Value, Environment.NewLine);
                            }
                        }
                    }
                    else
                    {
                        r.Append(" Status: ");
                        r.AppendLine(status.ToString());
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    r.AppendLine(" Status: Entry point not found");
                }
                catch (Exception e)
                {
                    r.AppendLine(" Status: " + e.Message);
                }

                r.AppendLine();
            }

            return r.ToString();
        }

        private void GetPMLog(ADLPMLogDataOutput data,
          ADLSensorType sensorType, ValueData sensor, float factor = 1.0f)
        {
            int i = (int)sensorType;
            if (i < data.Sensors.Length && data.Sensors[i].Supported)
            {
                sensor.Value = data.Sensors[i].Value * factor;
            }
        }

        public override void Update()
        {
            if (context != IntPtr.Zero && overdriveVersion >= 8 &&
              ADL.ADL2_New_QueryPMLogData_Get(context, adapterIndex,
              out var data) == ADLStatus.OK)
            {
                GetPMLog(data, ADLSensorType.TEMPERATURE_EDGE, temperatureCore);
                GetPMLog(data, ADLSensorType.TEMPERATURE_MEM, temperatureMemory);
                GetPMLog(data, ADLSensorType.TEMPERATURE_VRVDDC, temperatureVrmCore);
                GetPMLog(data, ADLSensorType.TEMPERATURE_VRMVDD, temperatureVrmMemory);
                GetPMLog(data, ADLSensorType.TEMPERATURE_VRMVDD0, temperatureVrmMemory0);
                GetPMLog(data, ADLSensorType.TEMPERATURE_VRMVDD1, temperatureVrmMemory1);
                GetPMLog(data, ADLSensorType.TEMPERATURE_VRSOC, temperatureVrmSoc);
                GetPMLog(data, ADLSensorType.TEMPERATURE_LIQUID, temperatureLiquid);
                GetPMLog(data, ADLSensorType.TEMPERATURE_PLX, temperaturePlx);
                GetPMLog(data, ADLSensorType.TEMPERATURE_HOTSPOT, temperatureHotSpot);
                GetPMLog(data, ADLSensorType.GFX_POWER, powerCore);
                GetPMLog(data, ADLSensorType.ASIC_POWER, powerTotal);
                GetPMLog(data, ADLSensorType.SOC_POWER, powerSoc);
                GetPMLog(data, ADLSensorType.FAN_RPM, fan);
                GetPMLog(data, ADLSensorType.CLK_GFXCLK, coreClock);
                GetPMLog(data, ADLSensorType.CLK_MEMCLK, memoryClock);
                GetPMLog(data, ADLSensorType.CLK_SOCCLK, socClock);
                GetPMLog(data, ADLSensorType.GFX_VOLTAGE, coreVoltage, 0.001f);
                GetPMLog(data, ADLSensorType.MEM_VOLTAGE, memoryVoltage, 0.001f);
                GetPMLog(data, ADLSensorType.SOC_VOLTAGE, socVoltage, 0.001f);
                GetPMLog(data, ADLSensorType.INFO_ACTIVITY_GFX, coreLoad);
                GetPMLog(data, ADLSensorType.INFO_ACTIVITY_MEM, memoryLoad);
                GetPMLog(data, ADLSensorType.FAN_PERCENTAGE, fanPercentage);
            }
            else
            {
                if (context != IntPtr.Zero && overdriveVersion >= 7)
                {
                    GetODNTemperature(ADLODNTemperatureType.CORE, temperatureCore);
                    GetODNTemperature(ADLODNTemperatureType.MEMORY, temperatureMemory);
                    GetODNTemperature(ADLODNTemperatureType.VRM_CORE, temperatureVrmCore);
                    GetODNTemperature(ADLODNTemperatureType.VRM_MEMORY, temperatureVrmMemory);
                    GetODNTemperature(ADLODNTemperatureType.LIQUID, temperatureLiquid);
                    GetODNTemperature(ADLODNTemperatureType.PLX, temperaturePlx);
                    GetODNTemperature(ADLODNTemperatureType.HOTSPOT, temperatureHotSpot);
                }
                else
                {
                    ADLTemperature adlt = new ADLTemperature();
                    if (ADL.ADL_Overdrive5_Temperature_Get(adapterIndex, 0, ref adlt)
                      == ADLStatus.OK)
                    {
                        temperatureCore.Value = 0.001f * adlt.Temperature;
                    }
                    else
                    {
                        temperatureCore.Value = null;
                    }
                }

                if (context != IntPtr.Zero && overdriveVersion >= 6)
                {
                    GetOD6Power(ADLODNCurrentPowerType.TOTAL_POWER, powerTotal);
                    GetOD6Power(ADLODNCurrentPowerType.CHIP_POWER, powerCore);
                    GetOD6Power(ADLODNCurrentPowerType.PPT_POWER, powerPpt);
                    GetOD6Power(ADLODNCurrentPowerType.SOCKET_POWER, powerSocket);
                }

                ADLFanSpeedValue adlf = new ADLFanSpeedValue();
                adlf.SpeedType = ADL.ADL_DL_FANCTRL_SPEED_TYPE_RPM;
                if (ADL.ADL_Overdrive5_FanSpeed_Get(adapterIndex, 0, ref adlf)
                  == ADLStatus.OK)
                {
                    fan.Value = adlf.FanSpeed;
                }
                else
                {
                    fan.Value = null;
                }

                adlf = new ADLFanSpeedValue();
                adlf.SpeedType = ADL.ADL_DL_FANCTRL_SPEED_TYPE_PERCENT;
                if (ADL.ADL_Overdrive5_FanSpeed_Get(adapterIndex, 0, ref adlf)
                  == ADLStatus.OK)
                {
                    fanPercentage.Value = adlf.FanSpeed;
                }
                else
                {
                    fanPercentage.Value = null;
                }

                ADLPMActivity adlp = new ADLPMActivity();
                if (ADL.ADL_Overdrive5_CurrentActivity_Get(adapterIndex, ref adlp)
                  == ADLStatus.OK)
                {
                    if (adlp.EngineClock > 0)
                    {
                        coreClock.Value = 0.01f * adlp.EngineClock;
                    }
                    else
                    {
                        coreClock.Value = null;
                    }

                    if (adlp.MemoryClock > 0)
                    {
                        memoryClock.Value = 0.01f * adlp.MemoryClock;
                    }
                    else
                    {
                        memoryClock.Value = null;
                    }

                    if (adlp.Vddc > 0)
                    {
                        coreVoltage.Value = 0.001f * adlp.Vddc;
                    }
                    else
                    {
                        coreVoltage.Value = null;
                    }

                    if (adlp.ActivityPercent >= 0 && adlp.ActivityPercent <= 100)
                    {
                        coreLoad.Value = adlp.ActivityPercent;
                    }
                    else
                    {
                        coreLoad.Value = null;
                    }
                }
                else
                {
                    coreClock.Value = null;
                    memoryClock.Value = null;
                    coreVoltage.Value = null;
                    coreLoad.Value = null;
                }
            }
        }

    }
}
