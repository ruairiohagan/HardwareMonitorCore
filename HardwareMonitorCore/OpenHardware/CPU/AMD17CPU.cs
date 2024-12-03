/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2020 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using HardwareMonitor.OpenHardware.HardwareHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8629 // Nullable value type may be null.

namespace OpenHardwareMonitor.Hardware.CPU
{

    internal sealed class AMD17CPU : AMDCPU
    {

        public readonly Core[] cores;

        public readonly Sensor coreTemperature;
        public readonly Sensor tctlTemperature;
        public readonly Sensor ccdMaxTemperature;
        public readonly Sensor ccdAvgTemperature;
        public readonly Sensor[] ccdTemperatures;
        public readonly Sensor packagePowerSensor;
        public readonly Sensor coresPowerSensor;
        public readonly Sensor busClock;

        private const uint FAMILY_17H_M01H_THM_TCON_TEMP = 0x00059800;
        private const uint FAMILY_17H_M01H_THM_TCON_TEMP_RANGE_SEL = 0x80000;
        private uint FAMILY_17H_M70H_CCD_TEMP(uint i) { return 0x00059954 + i * 4; }
        private const uint FAMILY_17H_M70H_CCD_TEMP_VALID = 0x800;
        private uint maxCcdCount;

        private const uint MSR_RAPL_PWR_UNIT = 0xC0010299;
        private const uint MSR_CORE_ENERGY_STAT = 0xC001029A;
        private const uint MSR_PKG_ENERGY_STAT = 0xC001029B;
        private const uint MSR_P_STATE_0 = 0xC0010064;
        private const uint MSR_FAMILY_17H_P_STATE = 0xc0010293;

        private float energyUnitMultiplier = 0;
        private uint lastEnergyConsumed;
        private DateTime lastEnergyTime;

        private readonly double timeStampCounterMultiplier;

        private struct TctlOffsetItem
        {
            public string Name { get; set; }
            public float Offset { get; set; }
        }
        private IEnumerable<TctlOffsetItem> tctlOffsetItems = new[] {
      new TctlOffsetItem { Name = "AMD Ryzen 5 1600X", Offset = 20.0f },
      new TctlOffsetItem { Name = "AMD Ryzen 7 1700X", Offset = 20.0f },
      new TctlOffsetItem { Name = "AMD Ryzen 7 1800X", Offset = 20.0f },
      new TctlOffsetItem { Name = "AMD Ryzen 7 2700X", Offset = 10.0f },
      new TctlOffsetItem { Name = "AMD Ryzen Threadripper 19", Offset = 27.0f },
      new TctlOffsetItem { Name = "AMD Ryzen Threadripper 29", Offset = 27.0f }
    };
        private readonly float tctlOffset = 0.0f;

        public AMD17CPU(int processorIndex, CPUID[][] cpuid)
          : base(processorIndex, cpuid)
        {
            string cpuName = cpuid[0][0].BrandString;
            if (!string.IsNullOrEmpty(cpuName))
            {
                foreach (var item in tctlOffsetItems)
                {
                    if (cpuName.StartsWith(item.Name))
                    {
                        tctlOffset = item.Offset;
                        break;
                    }
                }
            }

            coreTemperature = new Sensor() { Value = 0 };

            if (tctlOffset != 0.0f)
                tctlTemperature = new Sensor() { Value = 0 };

            ccdMaxTemperature = new Sensor() { Value = 0 };

            ccdAvgTemperature = new Sensor() { Value = 0 };

            switch (model & 0xf0)
            {
                case 0x30:
                case 0x70:
                    maxCcdCount = 8; break;
                default:
                    maxCcdCount = 4; break;
            }

            ccdTemperatures = new Sensor[maxCcdCount];
            for (int i = 0; i < ccdTemperatures.Length; i++)
            {
                ccdTemperatures[i] = new Sensor();
            }

            if (Ring0.Rdmsr(MSR_RAPL_PWR_UNIT, out uint eax, out _))
            {
                energyUnitMultiplier = 1.0f / (1 << (int)((eax >> 8) & 0x1F));
            }

            if (energyUnitMultiplier != 0)
            {
                if (Ring0.Rdmsr(MSR_PKG_ENERGY_STAT, out uint energyConsumed, out _))
                {
                    lastEnergyTime = DateTime.UtcNow;
                    lastEnergyConsumed = energyConsumed;
                    packagePowerSensor = new Sensor();
                }
            }
            coresPowerSensor = new Sensor();

            busClock = new Sensor();
            timeStampCounterMultiplier = GetTimeStampCounterMultiplier();
            if (timeStampCounterMultiplier > 0)
            {
                busClock.Value = (float)(TimeStampCounterFrequency /
                  timeStampCounterMultiplier);
            }

            this.cores = new Core[coreCount];
            for (int i = 0; i < this.cores.Length; i++)
            {
                this.cores[i] = new Core(i, cpuid[i], this);
            }
        }

        protected override uint[] GetMSRs()
        {
            return new uint[] { MSR_P_STATE_0, MSR_FAMILY_17H_P_STATE,
        MSR_RAPL_PWR_UNIT, MSR_CORE_ENERGY_STAT, MSR_PKG_ENERGY_STAT };
        }

        private IList<uint> GetSmnRegisters()
        {
            var registers = new List<uint>();
            registers.Add(FAMILY_17H_M01H_THM_TCON_TEMP);
            for (uint i = 0; i < maxCcdCount; i++)
            {
                registers.Add(FAMILY_17H_M70H_CCD_TEMP(i));
            }
            return registers;
        }

        public override string GetReport()
        {
            StringBuilder r = new StringBuilder();
            r.Append(base.GetReport());

            r.Append("Time Stamp Counter Multiplier: ");
            r.AppendLine(timeStampCounterMultiplier.ToString(
              CultureInfo.InvariantCulture));
            r.AppendLine();

            if (Ring0.WaitPciBusMutex(100))
            {
                r.AppendLine("SMN Registers");
                r.AppendLine();
                r.AppendLine(" Register  Value");
                var registers = GetSmnRegisters();

                for (int i = 0; i < registers.Count; i++)
                    if (ReadSmnRegister(registers[i], out uint value))
                    {
                        r.Append(" ");
                        r.Append(registers[i].ToString("X8", CultureInfo.InvariantCulture));
                        r.Append("  ");
                        r.Append(value.ToString("X8", CultureInfo.InvariantCulture));
                        r.AppendLine();
                    }
                r.AppendLine();

                Ring0.ReleasePciBusMutex();
            }

            return r.ToString();
        }

        private double GetTimeStampCounterMultiplier()
        {
            Ring0.Rdmsr(MSR_P_STATE_0, out uint eax, out _);
            uint cpuDfsId = (eax >> 8) & 0x3f;
            uint cpuFid = eax & 0xff;
            return 2.0 * cpuFid / cpuDfsId;
        }

        private bool ReadSmnRegister(uint address, out uint value)
        {
            if (!Ring0.WritePciConfig(0, 0x60, address))
            {
                value = 0;
                return false;
            }
            return Ring0.ReadPciConfig(0, 0x64, out value);
        }

        public override void Update()
        {
            base.Update();

            if (Ring0.WaitPciBusMutex(10))
            {

                uint value;
                if (ReadSmnRegister(FAMILY_17H_M01H_THM_TCON_TEMP, out value))
                {
                    float temperature = ((value >> 21) & 0x7FF) / 8.0f;
                    if ((value & FAMILY_17H_M01H_THM_TCON_TEMP_RANGE_SEL) != 0)
                        temperature -= 49;

                    if (tctlTemperature != null)
                    {
                        tctlTemperature.Value = temperature +
                          tctlTemperature.Value;
                    }

                    temperature -= tctlOffset;

                    coreTemperature.Value = temperature;
                }

                float maxTemperature = float.MinValue;
                int ccdCount = 0;
                float ccdTemperatureSum = 0;
                for (uint i = 0; i < ccdTemperatures.Length; i++)
                {
                    if (ReadSmnRegister(FAMILY_17H_M70H_CCD_TEMP(i), out value))
                    {
                        if ((value & FAMILY_17H_M70H_CCD_TEMP_VALID) == 0)
                            continue;

                        float temperature = (value & 0x7FF) / 8.0f - 49;
                        if (ccdTemperatures[i] != null)
                        {
                            temperature += ccdTemperatures[i].Value.Value;
                        }
                        if (temperature > maxTemperature)
                            maxTemperature = temperature;
                        ccdCount++;
                        ccdTemperatureSum += temperature;

                        ccdTemperatures[i].Value = temperature;
                    }
                }

                if (ccdCount > 1)
                {
                    ccdMaxTemperature.Value = maxTemperature;

                    ccdAvgTemperature.Value = ccdTemperatureSum / ccdCount;
                }

                Ring0.ReleasePciBusMutex();
            }

            if (energyUnitMultiplier != 0 &&
              Ring0.Rdmsr(MSR_PKG_ENERGY_STAT, out uint energyConsumed, out _))
            {
                DateTime time = DateTime.UtcNow;
                float deltaTime = (float)(time - lastEnergyTime).TotalSeconds;
                if (deltaTime > 0.01)
                {

                    packagePowerSensor.Value = energyUnitMultiplier * unchecked(
                      energyConsumed - lastEnergyConsumed) / deltaTime;
                    lastEnergyTime = time;
                    lastEnergyConsumed = energyConsumed;
                }
            }

            float? coresPower = 0f;
            for (int i = 0; i < cores.Length; i++)
            {
                cores[i].Update();
                coresPower += cores[i].Power;
            }
            coresPowerSensor.Value = coresPower;

        }

        public class Core
        {

            private readonly AMD17CPU cpu;
            private readonly GroupAffinity affinity;

            public readonly Sensor powerSensor;
            public readonly Sensor clockSensor;

            private DateTime lastEnergyTime;
            private uint lastEnergyConsumed;
            private float? power = null;

            public Core(int index, CPUID[] threads, AMD17CPU cpu)
            {
                this.cpu = cpu;
                this.affinity = threads[0].Affinity;

                string coreString = cpu.CoreString(index);
                this.powerSensor =
                  new Sensor();
                this.clockSensor =
                  new Sensor();

                if (cpu.energyUnitMultiplier != 0)
                {
                    if (Ring0.RdmsrTx(MSR_CORE_ENERGY_STAT, out uint energyConsumed,
                      out _, affinity))
                    {
                        lastEnergyTime = DateTime.UtcNow;
                        lastEnergyConsumed = energyConsumed;
                    }
                }
            }

            private double? GetMultiplier()
            {
                if (Ring0.Rdmsr(MSR_FAMILY_17H_P_STATE, out uint eax, out _))
                {
                    uint cpuDfsId = (eax >> 8) & 0x3f;
                    uint cpuFid = eax & 0xff;
                    return 2.0 * cpuFid / cpuDfsId;
                }
                else
                {
                    return null;
                }
            }

            public float? Power { get { return power; } }

            public void Update()
            {
                DateTime energyTime = DateTime.MinValue;
                double? multiplier = null;

                var previousAffinity = ThreadAffinity.Set(affinity);
                if (Ring0.Rdmsr(MSR_CORE_ENERGY_STAT, out uint energyConsumed, out _))
                {
                    energyTime = DateTime.UtcNow;
                }

                multiplier = GetMultiplier();
                ThreadAffinity.Set(previousAffinity);

                if (cpu.energyUnitMultiplier != 0)
                {
                    float deltaTime = (float)(energyTime - lastEnergyTime).TotalSeconds;
                    if (deltaTime > 0.01)
                    {
                        power = cpu.energyUnitMultiplier *
                          unchecked(energyConsumed - lastEnergyConsumed) / deltaTime;
                        powerSensor.Value = power;
                        lastEnergyTime = energyTime;
                        lastEnergyConsumed = energyConsumed;
                    }
                }

                if (multiplier.HasValue)
                {
                    float? clock = (float?)(multiplier * cpu.busClock.Value);
                    clockSensor.Value = clock;
                }
            }

        }
    }

}
