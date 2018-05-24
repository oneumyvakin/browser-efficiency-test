package main

import (
	"fmt"
	"os"
	"path/filepath"
	"strconv"
	"strings"
)

const (
	yandexBenchmark             = "YandexBenchmark"
	yandexBenchmarkColIdTest    = 0
	yandexBenchmarkColIdBrowser = 1
	yandexBenchmarkColIdMeasure = 2
)

func generateChartsForYandexBenchmarkFiles(files []os.FileInfo) error {
	measures := []Measure{}

	for _, f := range files {
		//fmt.Println(f.Name(), filepath.Ext(f.Name()))
		if filepath.Ext(f.Name()) != ".csv" {
			continue
		}

		if strings.HasPrefix(f.Name(), "YandexBenchmark") {
			m, err := yandexBenchmarkGetMeasures(filepath.Join(*csvPath, f.Name()))
			if err != nil {
				fmt.Printf("YandexBenchmark %s err %v", f.Name(), err)
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

	return nil
}

func yandexBenchmarkGetMeasures(csvFilePath string) ([]Measure, error) {
	msrs := []Measure{}

	csvFile, err := os.Open(csvFilePath)
	if err != nil {
		return msrs, err
	}

	records, err := getRecordsFromCsvFile(csvFile, ',')
	if err != nil {
		return nil, fmt.Errorf("getRecordsFromCsvFile %s err %v\n", csvFilePath, err)
	}

	for _, line := range records {
		val, err := strconv.ParseFloat(line[yandexBenchmarkColIdMeasure], 64)
		if err != nil {
			return nil, err
		}
		m := Measure{
			iteration: "0",
			//scenarioName: line[yandexBenchmarkColIdTest],
			measureSet: line[yandexBenchmarkColIdTest],
			//measureName: line[yandexBenchmarkColIdTest],
			browser: browserNameToProcessName[line[yandexBenchmarkColIdBrowser]],
			value:   val,
		}
		msrs = append(msrs, m)
	}
	//fmt.Printf("%#v\n", msrs)
	return msrs, nil
}
