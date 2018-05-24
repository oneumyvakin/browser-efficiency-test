package main

import (
	"bufio"
	"fmt"
	"io"
	"os"
	"path/filepath"
	"strconv"
	"strings"
)

const (
	intelPowerMeasureSet                   = "Intel Power"
	patternCumulativeGTEnergyJoules        = "Cumulative GT Energy_0 (Joules)"
	patternCumulativeGTEnergyMwh           = "Cumulative GT Energy_0 (mWh)"
	patternCumulativeProcessorEnergyJoules = "Cumulative Processor Energy_0 (Joules)"
	patternCumulativeProcessorEnergyMwh    = "Cumulative Processor Energy_0 (mWh)"
	patternCumulativeDramEnergyJoules      = "Cumulative DRAM Energy_0 (Joules)"
	patternCumulativeIaEnergyJoules        = "Cumulative IA Energy_0 (Joules)"
)

func generateChartsForIntelPowerLogFiles(files []os.FileInfo) error {
	measures := []Measure{}

	for _, f := range files {
		//fmt.Println(f.Name(), filepath.Ext(f.Name()))
		if filepath.Ext(f.Name()) != ".csv" {
			continue
		}

		if strings.Contains(f.Name(), "IntelPowerLog") {
			m, err := intelPowerLogGetMeasures(filepath.Join(*csvPath, f.Name()))
			if err != nil {
				fmt.Printf("IntelPowerLog %s err %v", f.Name(), err)
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

func groupMeasuresByIterations(measures []Measure) map[string]map[string][]Measure {
	browserResults := map[string]map[string][]Measure{}

	for _, m := range measures {
		browserProcessName := m.browser
		fullSetName := getMeasureSetFullName(
			m.measureSet, browserProcessName, m.measureName, m.scenarioName,
		) + " by iterations"

		if browserResults[fullSetName] == nil {
			browserResults[fullSetName] = make(map[string][]Measure)
		}

		browserResults[fullSetName][m.iteration] = append(browserResults[fullSetName][m.iteration], m)
	}
	//fmt.Printf("%#v\n", browserResults)
	return browserResults
}

//Search for total values like:
//Cumulative GT Energy_0 (Joules) = 1.712036
//Cumulative GT Energy_0 (mWh) = 0.475566
func intelPowerLogGetMeasures(csvFilePath string) ([]Measure, error) {
	msrs := []Measure{}

	f, err := os.Open(csvFilePath)
	if err != nil {
		return msrs, err
	}

	scanner := bufio.NewScanner(f)
	for scanner.Scan() {
		if err := scanner.Err(); err != nil {
			return msrs, err
		}
		m, err := intelPowerLogGetMeasureFromLogString(csvFilePath, scanner.Text())
		if err == io.EOF {
			continue
		}
		if err != nil {
			return msrs, err
		}
		msrs = append(msrs, m)
	}
	//fmt.Printf("%#v\n", msrs)
	return msrs, nil
}

func intelPowerLogGetMeasureFromLogString(csvFilePath, logString string) (Measure, error) {
	var m Measure
	var err error
	patterns := []string{
		patternCumulativeGTEnergyJoules,
		patternCumulativeProcessorEnergyJoules,
		patternCumulativeDramEnergyJoules,
		patternCumulativeIaEnergyJoules,
	}
	for _, pattern := range patterns {
		if strings.Contains(logString, pattern) {
			m, err = generalGetFileMeta(csvFilePath)
			if err != nil {
				return m, err
			}
			m.measureSet = intelPowerMeasureSet
			m.measureName = pattern
			val, err := intelPowerLogParse(logString)
			if err != nil {
				return m, err
			}
			m.value = val
			return m, nil
		}
	}
	return m, io.EOF
}

func intelPowerLogParse(token string) (float64, error) {
	matched := strings.Split(token, "=")
	for _, part := range matched {
		val, err := strconv.ParseFloat(strings.Trim(part, " "), 64)
		if err != nil {
			//fmt.Println(err)
			continue
		}
		return val, nil
	}

	return 0.0, fmt.Errorf("intelPowerLogParse value not found in '%s'", token)
}

func intelPowerLogGetFileMeta(csvFilePath string) (Measure, error) {
	base := filepath.Base(csvFilePath)
	fileMetaTokens := strings.Split(base, "_")
	m := Measure{}
	if len(fileMetaTokens) != 4 {
		return m, fmt.Errorf("intelPowerLogGetFileMeta not enough tokens in '%s'", csvFilePath)
	}
	m.browser = browserNameToProcessName[fileMetaTokens[1]]
	m.iteration = fileMetaTokens[2]
	m.scenarioName = fileMetaTokens[3]
	return m, nil
}
