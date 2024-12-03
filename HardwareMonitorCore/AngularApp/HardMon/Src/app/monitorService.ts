import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map, tap } from 'rxjs/operators';
import { BaseService } from './BaseService';

@Injectable({
    providedIn: 'root',
})

export class MonitorService extends BaseService {
    constructor(private http: HttpClient) {
        super();
    }

    public GetPerfCounterValue(category: string, counter: string, instance: string): Observable<number | null> {

        let urlParams = new URLSearchParams();
        urlParams.append('category', category);
        urlParams.append('counter', counter);
        urlParams.append('instance', instance);
        return this.http.get<number>(this.baseUrl + 'api/Perf/GetPerfCounterValue?' + urlParams.toString())
            .pipe(catchError(this.errorHandlerRX("GetPerfCounterValue", null)));
    }
    public GetSystemMemoryUsed(): Observable<number | null> {
        return this.http.get<number>(this.baseUrl + 'api/Perf/GetSystemMemoryUsed')
            .pipe(catchError(this.errorHandlerRX("GetSystemMemoryUsed", null)));
    }
    public GetGPUMemoryUsed(): Observable<number | null> {
        return this.http.get<number>(this.baseUrl + 'api/Perf/GetGPUMemoryUsed')
            .pipe(catchError(this.errorHandlerRX("GetGPUMemoryUsed", null)));
    }

    public GetDiskData(): Observable<DiskInfo[] | null> {
        return this.http.get<DiskInfo[]>(this.baseUrl + 'api/Perf/GetDiskData')
            .pipe(catchError(this.errorHandlerRX("GetDiskData", null)));
    }

    public GetCPUValues(): Observable<CpuInfo[] | null> {
        return this.http.get<CpuInfo[]>(this.baseUrl + 'api/IntelCPU/GetCPUValues')
            .pipe(catchError(this.errorHandlerRX("GetCPUValues", null)));
    }
    public GetAMDCPUValues(): Observable<AMDCpuInfo[] | null> {
        return this.http.get<AMDCpuInfo[]>(this.baseUrl + 'api/AMDCPU/GetCPUValues')
            .pipe(catchError(this.errorHandlerRX("GetCPUValues", null)));
    }
    public GetGPUValues(): Observable<AMDGPUInfo | null> {
        return this.http.get<AMDGPUInfo>(this.baseUrl + 'api/AMDGPU/GetGPUValues')
            .pipe(catchError(this.errorHandlerRX("GetGPUValues", null)));
    }
}
export interface ValueData {
    value: number;
}
export interface Sensor extends ValueData
{
}
export interface AMDCpuInfo {
    coreTemp: Sensor;
    packageTemp: Sensor;
    coreClocks: Sensor[];
    busClock: Sensor;
    powerInfo: Sensor;
}
export interface TempData extends ValueData {
    tempMax: number;
    tempSlope: number;
}
export interface ClockData extends ValueData {
}
export interface PowerData extends ValueData {
    Label: string;
    Index: number;
}
export interface CpuInfo {
    CoreTemps: TempData[];
    PackageTemp: TempData;
    CoreClocks: ClockData[];
    BusClock: ClockData;
    PowerInfo: PowerData[];
}
export interface AMDGPUInfo {
    temperatureCore: number;
    temperatureMemory: number;
    temperatureVrmCore: number;
    temperatureVrmMemory: number;
    temperatureVrmMemory0: number;
    temperatureVrmMemory1: number;
    temperatureLiquid: number;
    temperaturePlx: number;
    temperatureHotSpot: number;
    temperatureVrmSoc: number;
    powerCore: number;
    powerPpt: number;
    powerSocket: number;
    powerTotal: number;
    powerSoc: number;
    fan: number;
    fanPercentage: number;
    coreClock: number;
    memoryClock: number;
    socClock: number;
    coreVoltage: number;
    memoryVoltage: number;
    socVoltage: number;
    coreLoad: number;
    memoryLoad: number;
}
export interface DiskInfo {
    drive: string;
    util: number;
    totalBytesPerSec: number;
}
