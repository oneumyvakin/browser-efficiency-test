package main

import (
	"fmt"
	"os"
	"path/filepath"
	"strconv"
	"strings"
	"time"
)

func generateChartsForSrumFiles(files []os.FileInfo) error {
	measures := []Measure{}

	for _, f := range files {
		//fmt.Println(f.Name(), filepath.Ext(f.Name()))
		if filepath.Ext(f.Name()) != ".csv" {
			continue
		}

		if strings.Contains(f.Name(), "_srum_") {
			m, err := srumGetMeasures(filepath.Join(*csvPath, f.Name()))
			if err != nil {
				fmt.Printf("srum %s err %v", f.Name(), err)
				return err
			}
			measures = append(measures, m...)
		}
	}

	chartBars := getChartBarsFromRawResults(groupMeasuresBySet(measures), 2)

	for measureSet, barValues := range chartBars {
		err := drawBars(measureSet, barValues)
		if err != nil {
			return err
		}
	}

	groupedBars := getIterationsBarsFromGroupedMeasures(groupMeasuresByIterations(measures), 6)
	for measureSet, barValues := range groupedBars {
		err := drawBars(measureSet, barValues)
		if err != nil {
			return err
		}
	}

	return nil
}

const (
	srumMeasureSet                            = "srum"
	srumColIdAppId                            = 0
	srumColIdTimeStamp                        = 2
	srumColIdEnergyLoss                       = 12
	srumColIdCPUEnergyConsumption             = 13
	srumColIdSocEnergyConsumption             = 14
	srumColIdDisplayEnergyConsumption         = 15
	srumColIdDiskEnergyConsumption            = 16
	srumColIdNetworkEnergyConsumption         = 17
	srumColIdMBBEnergyConsumption             = 18
	srumColIdOtherEnergyConsumption           = 19
	srumColIdEmiEnergyConsumption             = 20
	srumColIdCPUEnergyConsumptionWorkOnBehalf = 21
	srumColIdCPUEnergyConsumptionAttributed   = 22
)

var srumMeasures = map[string]int{
	"EnergyLoss":                       srumColIdEnergyLoss,
	"CPUEnergyConsumption":             srumColIdCPUEnergyConsumption,
	"SocEnergyConsumption":             srumColIdSocEnergyConsumption,
	"DisplayEnergyConsumption":         srumColIdDisplayEnergyConsumption,
	"DiskEnergyConsumption":            srumColIdDiskEnergyConsumption,
	"NetworkEnergyConsumption":         srumColIdNetworkEnergyConsumption,
	"MBBEnergyConsumption":             srumColIdMBBEnergyConsumption,
	"OtherEnergyConsumption":           srumColIdOtherEnergyConsumption,
	"EmiEnergyConsumption":             srumColIdEmiEnergyConsumption,
	"CPUEnergyConsumptionWorkOnBehalf": srumColIdCPUEnergyConsumptionWorkOnBehalf,
	"CPUEnergyConsumptionAttributed":   srumColIdCPUEnergyConsumptionAttributed,
}

//
func srumGetMeasures(csvFilePath string) ([]Measure, error) {
	msrs := []Measure{}

	csvFile, err := os.Open(csvFilePath)
	if err != nil {
		return msrs, err
	}

	metaMeasure, err := srumGetFileMeta(csvFilePath)
	if err != nil {
		return nil, err
	}
	accum := map[string]float64{}

	records, err := getRecordsFromCsvFile(csvFile, ',')
	if err != nil {
		return nil, fmt.Errorf("srumGetMeasures.getRecordsFromCsvFile %s err %v\n", csvFilePath, err)
	}

	//fmt.Printf("%#v\n", metaMeasure)
	for i := len(records) - 1; i >= 0; i-- {
		line := records[i]
		//fmt.Printf("%s\n", line[srumColIdTimeStamp])
		if line[srumColIdTimeStamp] == " TimeStamp" {
			break
		}

		// 2017-12-11:01:30:30.0000
		t, err := time.Parse("2006-01-02:15:04:05.0000", strings.Trim(line[srumColIdTimeStamp], " "))
		if err != nil {
			return nil, err
		}

		//fmt.Printf("Meta: %s Measure: %s\n", metaMeasure.date.String(), t.String())
		if t.Before(metaMeasure.date) {
			break
		}

		if !strings.Contains(line[srumColIdAppId], metaMeasure.browser) {
			continue
		}

		for measureName, colId := range srumMeasures {
			val, err := strconv.ParseFloat(strings.Trim(line[colId], " "), 64)
			if err != nil {
				return nil, err
			}

			accum[measureName] += val
		}
	}

	for measureName, measureVal := range accum {
		if measureVal == 0 {
			continue
		}
		m := metaMeasure
		m.measureName = measureName
		m.value = measureVal
		msrs = append(msrs, m)
	}

	//fmt.Printf("%#v\n", msrs)
	return msrs, nil
}

func srumGetFileMeta(csvFilePath string) (Measure, error) {
	base := filepath.Base(csvFilePath)
	fileMetaTokens := strings.Split(base, "_")
	m := Measure{}
	if len(fileMetaTokens) != 6 {
		return m, fmt.Errorf("srumGetFileMeta not match tokens count in '%s': %d != %d", base, len(fileMetaTokens), 6)
	}
	// chrome_yandexstaticfavicon_0_srum_20171217_010658.csv
	m.browser = browserNameToProcessName[fileMetaTokens[0]]
	m.scenarioName = fileMetaTokens[1]
	m.iteration = fileMetaTokens[2]
	m.measureSet = fileMetaTokens[3]
	// 20171217_010658
	t, err := time.ParseInLocation("20060102150405.csv", fileMetaTokens[4]+fileMetaTokens[5], time.Local)
	if err != nil {
		return m, fmt.Errorf("srumGetFileMeta time.Parse(\"20060102150405\", %s) failed: '%s'", fileMetaTokens[4]+fileMetaTokens[5], err)
	}
	m.date = t
	return m, nil
}
