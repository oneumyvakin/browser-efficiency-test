package main

import (
	"fmt"
	"os"
	"path/filepath"
	"strconv"
	"strings"
)

func generateChartsForIppetFiles(files []os.FileInfo) error {
	var measures []Measure

	for _, f := range files {
		//fmt.Println(f.Name(), filepath.Ext(f.Name()))
		if filepath.Ext(f.Name()) != ".xls" {
			continue
		}

		if strings.Contains(f.Name(), "ippet") && strings.Contains(f.Name(), "_processes") {
			m, err := ippetGetMeasures(filepath.Join(*csvPath, f.Name()))
			if err != nil {
				fmt.Printf("ippet %s err %v", f.Name(), err)
				return err
			}
			measures = append(measures, m...)
		}
	}

	chartBars := getChartBarsFromRawResults(groupMeasuresBySet(measures), 6)

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

func ippetGetMeasures(csvFilePath string) ([]Measure, error) {
	csvFile, err := os.Open(csvFilePath)
	if err != nil {
		return nil, err
	}

	metaMeasure, err := generalGetFileMeta(csvFilePath)
	if err != nil {
		return nil, err
	}

	records, err := getRecordsFromCsvFile(csvFile, '\t')
	if err != nil {
		return nil, fmt.Errorf("ippetGetMeasures: %s err %v\n", csvFilePath, err)
	}

	// Accumulates cols of desired measures types
	patternsPerSystem := map[string][]int{
		"Power(_Total)\\Package W": {}, // Whole system
		"Power(_Total)\\CPU W":     {}, // Whole system
		"Power(_Total)\\GPU W":     {}, // Whole system
		"Power(_Total)\\Disk W":    {}, // Whole system
	}
	patternsPerProcess := map[string][]int{
		"GPU Power W": {}, // Per process
		"CPU Power W": {}, // Per process
		")\\%CPU":     {}, // Per process
	}

	// Determine cols IDs with required measures
	for colId, colTitle := range records[0] { // 0 line for headers
		// Per system
		for pattern, _ := range patternsPerSystem {
			if strings.Contains(colTitle, pattern) {
				patternsPerSystem[pattern] = append(patternsPerSystem[pattern], colId)
			}
		}

		// Per browser process name
		for pattern, _ := range patternsPerProcess {
			if strings.Contains(colTitle, pattern) {
				for _, browserProcessName := range metaMeasure.browserProcesses {
					if strings.Contains(colTitle, browserProcessName) {
						patternsPerProcess[pattern] = append(patternsPerProcess[pattern], colId)
						// fmt.Println(pattern, colId, patternsPerProcess[pattern])
					}
				}
			}
		}

	}

	// Merge
	colIdsOfMeasures := map[string][]int{}
	for measureName, colIds := range patternsPerSystem {
		colIdsOfMeasures[strings.Replace(measureName, "Power(_Total)\\", "Power Total", 1)] = colIds
	}
	for measureName, colIds := range patternsPerProcess {
		colIdsOfMeasures[strings.Replace(measureName, ")\\%CPU", "CPU Usage Percents", 1)] = colIds
	}

	// Walk over lines cols to gather measures
	accum := map[string]float64{}
	for i := 1; i < len(records); i++ { // 1 for rest of lines except headers line
		line := records[i]
		for measureName, colIds := range colIdsOfMeasures {

			for _, colId := range colIds {
				val, err := strconv.ParseFloat(strings.Trim(line[colId], " "), 64)
				if err != nil {
					return nil, fmt.Errorf("ippetGetMeasures: %s ParseFloat err %v\n", csvFilePath, err)
				}

				accum[measureName] += val
			}
			//fmt.Println(measureName, accum[measureName], colIds)
		}
	}

	//fmt.Printf("%#v\n", metaMeasure)
	var msrs []Measure
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
