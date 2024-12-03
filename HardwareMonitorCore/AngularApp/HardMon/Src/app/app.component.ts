import { Component } from '@angular/core';
import { Sensor, MonitorService, AMDCpuInfo, PowerData, AMDGPUInfo, DiskInfo } from './monitorService';
import { Color, ECharts, EChartsOption, EChartsType, getInstanceByDom, SeriesOption, TreemapSeriesOption } from 'echarts';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css']
})
export class AppComponent {
    title = 'HardMon';
    public refreshIntervalMS: number = 1000;
    public refreshTimer = setInterval(() => { }, this.refreshIntervalMS);
    protected gRed: Color = "Red";
    protected gAmber: Color = "Orange";
    protected gGreen: Color = "Green";

    public mbUp: number = 0;
    public mbDown: number = 0;

    public cpuUtil: number = 0;
    public cpuData?: AMDCpuInfo;
    public gpuData?: AMDGPUInfo;
    public cpuHistory: number[] = [];
    protected cpuGaugeChart!: ECharts;
    public cpuGaugeOptionActual =
        {
            data: [{
                name: 'CPU Utilisation',
                value: this.cpuUtil
            }],
            type: 'gauge',
            center: ['50%', '70%'],
            startAngle: 200,
            endAngle: -20,
            min: 0,
            max: 100,
            radius: 95,
            splitNumber: 5,
            itemStyle: {
                color: 'auto'
            },
            progress: {
                show: true,
                width: 30,
                itemStyle: {
                    color: 'black'
                }
            },
            pointer: {
                show: false
            },
            axisLine: {
                lineStyle: {
                    color: [
                        [0.3, this.gGreen],
                        [0.7, this.gAmber],
                        [1, this.gRed]
                    ],
                    width: 3
                }
            },
            axisTick: {
                distance: 0,
                splitNumber: 2,
                lineStyle: {
                    width: 2,
                    color: '#999'
                }
            },
            splitLine: {
                distance: 0,
                length: 14,
                lineStyle: {
                    width: 3,
                    color: '#999'
                }
            },
            axisLabel: {
                distance: -40,
                color: '#999',
                fontSize: 20
            },
            anchor: {
                show: false
            },
            title: {
                show: false
            },
            detail: {
                valueAnimation: true,
                width: '60%',
                lineHeight: 40,
                borderRadius: 8,
                offsetCenter: [0, '-15%'],
                fontSize: 36,
                fontWeight: 'normal',
                formatter: 'CPU\n{value}%',
                color: 'white'
            }
        };
    public cpuGaugeOption: EChartsOption = { series: [] };

    protected gpuGaugeChart!: ECharts;
    public get gpuPower(): number {
        return this.gpuData?.powerTotal ?? 0;
    };
    public gpuGaugeOptionActual =
        {
            data: [{
                name: 'GPU Power',
                value: this.gpuPower
            }],
            type: 'gauge',
            center: ['50%', '70%'],
            startAngle: 200,
            endAngle: -20,
            min: 0,
            max: 300,
            radius: 95,
            splitNumber: 3,
            itemStyle: {
                color: 'auto'
            },
            progress: {
                show: true,
                width: 30,
                itemStyle: {
                    color: 'black'
                }
            },
            pointer: {
                show: false
            },
            axisLine: {
                lineStyle: {
                    color: [
                        [0.3, this.gGreen],
                        [0.7, this.gAmber],
                        [1, this.gRed]
                    ],
                    width: 3
                }
            },
            axisTick: {
                distance: 0,
                splitNumber: 2,
                lineStyle: {
                    width: 2,
                    color: '#999'
                }
            },
            splitLine: {
                distance: 0,
                length: 10,
                lineStyle: {
                    width: 3,
                    color: '#999'
                }
            },
            axisLabel: {
                distance: -50,
                color: '#999',
                fontSize: 20
            },
            anchor: {
                show: false
            },
            title: {
                show: false
            },
            detail: {
                valueAnimation: true,
                width: '60%',
                lineHeight: 40,
                borderRadius: 8,
                offsetCenter: [0, '-15%'],
                fontSize: 36,
                fontWeight: 'normal',
                formatter: 'GPU\n{value}W',
                color: 'white'
            }
        };
    public gpuGaugeOption: EChartsOption = { series: [] };

    public onCPUGaugeInit(event: any) {
        this.cpuGaugeChart = event;
    }

    public onGPUGaugeInit(event: any) {
        this.gpuGaugeChart = event;
    }

    protected cpuLineChart!: ECharts;
    public cpuLineOption: EChartsOption = {
        xAxis: {
            type: 'category',
            show: false
        },
        yAxis: {
            type: 'value',
            min: 0,
            max: 100,
            interval: 50,
            splitLine: {
                lineStyle: {
                    color: 'grey'
                }
            }
        },
        color: 'red',
        grid: {
            height: '90%',
            top: 10
        },
        visualMap: {
            pieces: [
                {
                    gt: 0,
                    lte: 30,
                    color: '#008000'
                },
                {
                    gt: 30,
                    lte: 70,
                    color: '#ff8040'
                },
                {
                    gt: 70,
                    lte: 100,
                    color: '#FF0000'
                }
            ],
            outOfRange: {
                color: '#999'
            },
            show: false
        },
        series: [
            {
                data: this.cpuHistory,
                type: 'line',
                symbol: 'none',
                showSymbol: false,
                markLine: {
                    silent: true
                },
                areaStyle: {}
            }
        ],        
        animationEasing: 'elasticIn'
    };
    public onCPUChartInit(event: any) {
        this.cpuLineChart = event;
    }

    public diskData: any[] = [];
    protected diskTreeChart!: ECharts;

    public diskBarOption: EChartsOption =
        {
            xAxis: {
                show: false,
                type: 'category',
                data: ['%']
            },
            yAxis: {
                show: false,
                type: 'value'
            },
            grid: {
                height: '95%',
                width: '95%',
                top: 10
            },
            series: []
        };
    public onDiskTreeInit(event: any) {
        this.diskTreeChart = event;
    }    

    constructor(protected monitorService: MonitorService) {
        this.cpuGaugeOption.series = this.cpuGaugeOptionActual as SeriesOption;
        this.gpuGaugeOption.series = this.gpuGaugeOptionActual as SeriesOption;

        this.refreshTimer = setInterval(() => { this.RefreshData() }, this.refreshIntervalMS);
    }

    protected round(x: number, n: number): number {
        var factor = Math.pow(10, n);
        x *= factor;
        x = Math.round(x);
        x /= factor;
        return x;
    }
    protected GetLightsColour(value: number, maxVal: number = 100): Color {
        if (value < maxVal * .3) return this.gGreen;
        if (value < maxVal * .7) return this.gAmber;
        return this.gRed;
    }
    public systemMemUsed: number = 0;
    public gpuMemory: number = 0;
    protected RefreshData() {
        this.monitorService.GetPerfCounterValue("Processor Information", "% Processor Utility", "_Total").subscribe(result => {

            if (result != null) {
                this.cpuUtil = this.round(result, 1);
                this.cpuGaugeOptionActual.data = [{
                    name: 'CPU Utilisation',
                    value: this.cpuUtil
                }];

                this.cpuHistory.push(result!);
                if (this.cpuHistory.length > 20) {
                    this.cpuHistory.splice(0, 1);
                }
                this.cpuLineChart.setOption(this.cpuLineOption);

                this.cpuGaugeOptionActual.progress.itemStyle.color = this.GetLightsColour(this.cpuUtil).toString();
                this.cpuGaugeChart.setOption(this.cpuGaugeOption);
            }
        });
        this.monitorService.GetPerfCounterValue("Network Interface", "Bytes Sent/sec", "Realtek Gaming 2.5GbE Family Controller").subscribe(result => {
            if (result != null) {
                result /= (1024 * 1024);
                result = this.round(result, 2) * 8; //Want to show M bits per sec
                this.mbUp = result;
            }
        });

        this.monitorService.GetPerfCounterValue("Network Interface", "Bytes Received/sec", "Realtek Gaming 2.5GbE Family Controller").subscribe(result => {
            if (result != null) {
                result /= (1024 * 1024);
                result = this.round(result, 2) * 8; //Want to show M bits per sec
                this.mbDown = result;
            }
        });

        this.monitorService.GetGPUMemoryUsed().subscribe(result => {
            if (result != null) {
                result /= (1024 * 1024 * 1024);
                result = this.round(result, 1);
                this.gpuMemory = result;
            }
        });

        this.monitorService.GetAMDCPUValues().subscribe(result => {
            if (result && result.length > 0) {
                this.cpuData = result[0];
            }
        });

        this.monitorService.GetGPUValues().subscribe(result => {
            if (result) {
                this.gpuData = result;

                this.gpuGaugeOptionActual.progress.itemStyle.color = this.GetLightsColour(this.gpuPower, 300).toString();
                this.gpuGaugeOptionActual.data[0].value = this.gpuPower;
                this.gpuGaugeChart.setOption(this.gpuGaugeOption);
            }
        });
        this.monitorService.GetSystemMemoryUsed().subscribe(result => {
            if (result) {
                this.systemMemUsed = this.round(result / (1024 * 1024 * 1024), 1);
            }
        });

        this.monitorService.GetDiskData().subscribe(result => {
            if (result) {
                var dSeries: SeriesOption[] = [];

                var totalUtil: number = 0;
                this.diskData.length = 0;
                result.forEach(di => {
                    var mbSec: number = di.totalBytesPerSec / (1024 * 1024);
                    mbSec = this.round(mbSec, 1);
                    dSeries[dSeries.length] = {
                        type: 'bar',
                        name: `${di.drive}\n${this.round(di.util, 1)}%\n${mbSec}Mb/sec`,
                        stack: 'total',
                        label: {
                            show: di.util > 2,
                            verticalAlign: 'middle',                            
                            formatter: `${di.drive} ${this.round(di.util, 1)}%\n${mbSec}M/s`,
                            fontSize: 35
                        },
                        data: [this.round(di.util, 1)],
                        silent: true
                    };
                    totalUtil += di.util;
                });

                var idleUtil = 0;
                if (totalUtil < 10) {
                    idleUtil = 10 - this.round(totalUtil, 1);
                }
                // Add a "Disks are idle" disk
                dSeries[dSeries.length] = {
                    type: 'bar',
                    name: "Disks\nidle",
                    stack: 'total',
                    label: {
                        show: idleUtil > 2,
                        verticalAlign: 'middle',
                        formatter: 'Disks\nIdle',
                        fontSize:40
                    },
                    data: [idleUtil],
                    silent: true
                }

                this.diskBarOption.series = dSeries;
                this.diskTreeChart.setOption(this.diskBarOption);
            }
        })
    }
    public get cpuPower(): number{
        if (!this.cpuData?.powerInfo) return 0;

        var packagePower: Sensor = this.cpuData.powerInfo;
        var watts: number = packagePower?.value ?? 0;
        return this.round(watts, 0);
    }
    public get cpuTemp(): number {
        if (!this.cpuData?.packageTemp) return 0;

        var temp: number = this.cpuData.packageTemp.value ?? 0;
        return this.round(temp, 0);
    }
    public get gpuTemp(): number {
        if (!this.gpuData?.temperatureCore) return 0;

        var temp: number = this.gpuData?.temperatureCore ?? 0;
        return this.round(temp, 0);
    }
    public get gpuHotTemp(): number {
        if (!this.gpuData?.temperatureHotSpot) return 0;

        var temp: number = this.gpuData?.temperatureHotSpot?? 0;
        return this.round(temp, 0);
    }
    public get gpuFanRPM(): number {
        if (!this.gpuData?.fan) return 0;

        var fanRPM: number = this.gpuData?.fan ?? 0;
        return this.round(fanRPM, 0);
    }
}
