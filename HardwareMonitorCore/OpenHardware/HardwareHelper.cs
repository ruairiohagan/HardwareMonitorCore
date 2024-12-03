using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace HardwareMonitor.OpenHardware.HardwareHelper
{
    [DataContractAttribute]
    public class ValueData
    {
        [DataMemberAttribute]
        public float? Value { get; set; } = null;
    }
    [DataContractAttribute]
    public class TempData : ValueData
    {
        public TempData()
        {
        }
        public TempData(float tMax, float slope)
        {
            tempMax = tMax;
            tempSlope = slope;
        }
        [DataMemberAttribute]
        public float tempMax { get; set; } = 0.0F;
        [DataMemberAttribute]
        public float tempSlope { get; set; } = 0.0F;
    }
    public class Sensor : ValueData
    {
    }
    [DataContractAttribute]
    public class PowerData : ValueData
    {
        public PowerData()
        {
        }
        public PowerData(string label, int index)
        {
            Label = label;
            Index = index;
        }
        [DataMemberAttribute]
        public string? Label { get; set; }
        [DataMemberAttribute]
        public int Index { get; set; }
    }

    [DataContractAttribute]
    public class FanData : ValueData
    {
        [DataMemberAttribute]
        public string? Label { get; set; }

    }
    [DataContractAttribute]
    public class VoltageData : ValueData
    {
        [DataMemberAttribute]
        public string? Label { get; set; }

    }
    [DataContractAttribute]
    public class LoadData : ValueData
    {
        [DataMemberAttribute]
        public string? Label { get; set; }

    }
}