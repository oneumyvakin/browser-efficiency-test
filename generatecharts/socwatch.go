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
	socWatch                                      = "socwatch"
	socWatchWakeupAnalysis                        = "WakeupAnalysis"
	socWatchWakeupAnalysisContextSwitchStatistics = "Context Switch (in) Statistics"
	/*
		Process,Total Context Switches,Caused Thread Wakeups,Caused Core Wakeups,Caused Package Wakeups,
		brodefault.exe(1232),7371,1434( 19.45%),274(  3.72%),31(  0.42%),
		brodefault.exe(8660),6822,286(  4.19%),57(  0.84%),10(  0.15%),
		brodefault.exe(9244),431,4(  0.93%),1(  0.23%),0(  0.00%),
		brodefault.exe(9608),2963,253(  8.54%),16(  0.54%),1(  0.03%),
	*/
	socWatchWakeupAnalysisProcessesBusyDuration = "Processes by Platform Busy Duration,"
	/*
		Rank,Process Name (PID),CPU % (Platform),Duration in ms (Platform),CPU % (Logical),Duration in ms (Logical),CSwitches From Idle (per sec),
		6,brodefault.exe (8660),6.66,1004.54,1.74,1051.67,30.05,
		7,brodefault.exe (1232),5.87,884.20,1.86,1120.95,119.87,
	*/
	socWatchWakeupAnalysisTimerResolutionRequestsTime = "Timer Resolution Requests (OS) Summary: Residency (Time)"
	/*
		Kernel/Application  , 1.0ms (msec)
		------------------  , ------------
		websrv.exe(9556)    , 14.53
		brodefault.exe(9608), 634.90
		brodefault.exe(8660), 1135.94
		brodefault.exe(1232), 2504.15
		brodefault.exe(9244), 36.27
	*/
	socWatchWakeupAnalysisTimerResolutionRequestsCount = "Timer Resolution Requests (OS) Summary: Entry Counts"
	/*
		Requested Resolution, websrv.exe(3772) Entry Count, browser.exe(11248) Entry Count, browser.exe(6072) Entry Count
		--------------------, ----------------------------, ------------------------------, -----------------------------
		1.0ms               , 8                           , 114                           , 6
		Total               , 8                           , 114                           , 6
	*/
)

func generateChartsForSocWatchFiles(files []os.FileInfo) error {
	var measures []Measure

	for _, f := range files {
		if filepath.Ext(f.Name()) != ".csv" {
			continue
		}

		filePath := filepath.Join(*csvPath, socWatch, f.Name())
		m, err := socWatchGetMeasures(filePath)
		if err != nil {
			return fmt.Errorf("%s: %v", filePath, err)
		}
		measures = append(measures, m...)
	}

	chartBars := getChartBarsFromRawResults(groupMeasuresBySet(measures), 2)

	for measureSet, barValues := range chartBars {
		err := drawBars(measureSet, barValues)
		if err != nil {
			return err
		}
	}

	groupedBars := getIterationsBarsFromGroupedMeasures(groupMeasuresByIterations(measures), 2)
	for measureSet, barValues := range groupedBars {
		err := drawBars(measureSet, barValues)
		if err != nil {
			return err
		}
	}

	return nil
}

func socWatchGetMeasures(csvFilePath string) ([]Measure, error) {
	csvFile, err := os.Open(csvFilePath)
	if err != nil {
		return nil, err
	}
	defer csvFile.Close()

	metaMeasure, err := generalGetFileMeta(csvFilePath)
	if err != nil {
		return nil, err
	}

	dataParts := map[string]string{
		socWatchWakeupAnalysisContextSwitchStatistics:      "",
		socWatchWakeupAnalysisTimerResolutionRequestsCount: "",
		socWatchWakeupAnalysisTimerResolutionRequestsTime:  "",
		socWatchWakeupAnalysisProcessesBusyDuration:        "",
	}

	scanner := bufio.NewReader(csvFile)
	for {
		line, err := scanner.ReadString('\n')
		if err == io.EOF {
			break
		}
		if err != nil {
			fmt.Printf("%s: %v\n", csvFilePath, err)
			break
		}

		for part, _ := range dataParts {
			if line == fmt.Sprintf("%s\n", part) || line == fmt.Sprintf("%s,\r\n", part) {
				for {
					dataline, err := scanner.ReadString('\n')
					if err == io.EOF {
						break
					}
					if err != nil {
						fmt.Printf("in file: %s\nin part: %s:\nerror: %v\n", csvFilePath, part, err)
						break
					}
					if dataline == "\r\n" || dataline == "\n" || dataline == "" {
						break
					}
					//fmt.Printf("in file: %s\nin part: %s:\ndataline: %s\nerr: %v\n", csvFilePath, part, dataline, err)
					dataParts[part] += strings.Trim(dataline, " ")
				}
			}
		}
	}

	processParts := map[string]func([][]string, Measure) []Measure{
		socWatchWakeupAnalysisContextSwitchStatistics:      socWatchWakeupAnalysisContextSwitchStatisticsFunc,
		socWatchWakeupAnalysisTimerResolutionRequestsCount: socWatchWakeupAnalysisTimerResolutionRequestsCountFunc,
		socWatchWakeupAnalysisTimerResolutionRequestsTime:  socWatchWakeupAnalysisTimerResolutionRequestsTimeFunc,
		socWatchWakeupAnalysisProcessesBusyDuration:        socWatchWakeupAnalysisProcessesBusyDurationFunc,
	}

	//fmt.Printf("%#v\n", metaMeasure)
	var msrs []Measure

	for part, f := range processParts {
		records, err := getRecordsFromString(dataParts[part], ',')
		if err != nil {
			fmt.Printf("%s\n%s\n%s\n", csvFilePath, part, err)
			continue
		}
		if len(records) > 0 {
			//fmt.Printf("%s \n %s \n %s:\n", csvFilePath, part)
			tmpMsrs := f(records, metaMeasure)
			msrs = append(msrs, tmpMsrs...)
		}
	}

	//fmt.Printf("%#v\n", msrs)
	return msrs, nil
}

func socWatchWakeupAnalysisProcessesBusyDurationFunc(records [][]string, meta Measure) []Measure {
	/*
		Processes by Platform Busy Duration,
		Rank,Process Name (PID),CPU % (Platform),Duration in ms (Platform),CPU % (Logical),Duration in ms (Logical),CSwitches From Idle (per sec),
		,Overall Platform Activity,22.93,23400.23,9.75,39813.78,1497.21,
		1,dwm.exe (1164),6.12,6241.95,1.66,6756.53,173.45,
		2,MicrosoftEdge.exe (14828),5.07,5173.71,1.54,6280.26,80.33,
		9,MicrosoftEdgeCP.exe (15220),0.98,998.43,0.25,1015.16,2.58,
	*/
	var msrs []Measure
	colProcess := 1
	cols := map[string]int{
		"Duration in ms (Platform)":     3,
		"Duration in ms (Logical)":      5,
		"CSwitches From Idle (per sec)": 6,
	}
	mapMsrs := map[string]*Measure{}
	for msrName, _ := range cols {
		mapMsrs[msrName] = &Measure{
			browser:          meta.browser,
			browserShortName: meta.browserShortName,
			browserProcesses: meta.browserProcesses,
			date:             meta.date,
			iteration:        meta.iteration,
			measureName:      msrName,
			measureSet:       socWatch,
			scenarioName:     meta.scenarioName,
			value:            0,
		}
	}
	for _, row := range records {
		//fmt.Printf("'%s': %s\n", row[colProcess], meta.browser)
		for _, browserProcessName := range meta.browserProcesses {
			if strings.HasPrefix(row[colProcess], browserProcessName) {
				for msrName, colId := range cols {
					val, err := strconv.ParseFloat(strings.Trim(strings.Split(row[colId], "(")[0], " "), 64)
					if err != nil {
						fmt.Printf("failed parse '%s': %v\n", row[colId], err)
						continue
					}
					mapMsrs[msrName].value += val
				}
			}
		}
	}

	for _, msr := range mapMsrs {
		if msr.value > 0 {
			msrs = append(msrs, *msr)
		}
	}

	return msrs
}

func socWatchWakeupAnalysisTimerResolutionRequestsCountFunc(records [][]string, meta Measure) []Measure {
	/*
		Requested Resolution, websrv.exe(3772) Entry Count, browser.exe(11248) Entry Count, browser.exe(6072) Entry Count
		--------------------, ----------------------------, ------------------------------, -----------------------------
		1.0ms               , 8                           , 114                           , 6
		Total               , 8                           , 114                           , 6
	*/
	var msrs []Measure
	colResolution := 0
	var cols []int
	headers := records[0]
	//fmt.Printf("headers '%#v'\n", headers)
	for index, header := range headers {
		for _, browserProcessName := range meta.browserProcesses {
			if strings.HasPrefix(strings.Trim(header, " "), browserProcessName) {
				cols = append(cols, index)
				break
			}
		}
	}

	mapMsrs := map[string]*Measure{}
	for index, row := range records {
		if index == 0 || index == 1 {
			continue
		}

		mapMsrs[row[colResolution]] = &Measure{
			browser:          meta.browser,
			browserShortName: meta.browserShortName,
			browserProcesses: meta.browserProcesses,
			date:             meta.date,
			iteration:        meta.iteration,
			measureName:      "Timer Resolution Count " + strings.Trim(row[colResolution], " "),
			measureSet:       socWatch,
			scenarioName:     meta.scenarioName,
			value:            0,
		}

		for _, colId := range cols {
			val, err := strconv.ParseFloat(strings.Trim(row[colId], " "), 64)
			if err != nil {
				fmt.Printf("failed parse '%s': %v\n", row[colId], err)
				continue
			}
			mapMsrs[row[colResolution]].value += val
		}
	}

	for _, msr := range mapMsrs {
		if msr.value > 0 {
			msrs = append(msrs, *msr)
		}
	}

	return msrs
}

func socWatchWakeupAnalysisContextSwitchStatisticsFunc(records [][]string, meta Measure) []Measure {
	/*
		Process,Total Context Switches,Caused Thread Wakeups,Caused Core Wakeups,Caused Package Wakeups,
		brodefault.exe(1232),7371,1434( 19.45%),274(  3.72%),31(  0.42%),
		brodefault.exe(8660),6822,286(  4.19%),57(  0.84%),10(  0.15%),
		brodefault.exe(9244),431,4(  0.93%),1(  0.23%),0(  0.00%),
		brodefault.exe(9608),2963,253(  8.54%),16(  0.54%),1(  0.03%),
	*/
	var msrs []Measure
	colProcess := 0
	cols := map[string]int{
		"Total Context Switches": 1,
		"Caused Thread Wakeups":  2,
		"Caused Core Wakeups":    3,
		"Caused Package Wakeups": 4,
	}
	mapMsrs := map[string]*Measure{}
	for msrName, _ := range cols {
		mapMsrs[msrName] = &Measure{
			browser:          meta.browser,
			browserShortName: meta.browserShortName,
			browserProcesses: meta.browserProcesses,
			date:             meta.date,
			iteration:        meta.iteration,
			measureName:      msrName,
			measureSet:       socWatch,
			scenarioName:     meta.scenarioName,
			value:            0,
		}
	}
	for _, row := range records {
		//fmt.Printf("'%s': %s\n", row[colProcess], meta.browser)
		for _, browserProcessName := range meta.browserProcesses {
			if strings.HasPrefix(row[colProcess], browserProcessName) {
				for msrName, colId := range cols {
					val, err := strconv.ParseFloat(strings.Trim(strings.Split(row[colId], "(")[0], " "), 64)
					if err != nil {
						fmt.Printf("failed parse '%s': %v\n", row[colId], err)
						continue
					}
					mapMsrs[msrName].value += val
				}
			}
		}
	}

	for _, msr := range mapMsrs {
		if msr.value > 0 {
			msrs = append(msrs, *msr)
		}
	}

	return msrs
}

func socWatchWakeupAnalysisTimerResolutionRequestsTimeFunc(records [][]string, meta Measure) []Measure {
	//fmt.Printf("records '%s': %v\n", records)
	var msrs []Measure
	colProcess := 0
	cols := map[string]int{}
	headers := records[0]

	for index, header := range headers {
		if index == 0 {
			continue
		}
		cols[header] = index
	}

	mapMsrs := map[string]*Measure{}
	for msrName, _ := range cols {
		mapMsrs[msrName] = &Measure{
			browser:          meta.browser,
			browserShortName: meta.browserShortName,
			browserProcesses: meta.browserProcesses,
			date:             meta.date,
			iteration:        meta.iteration,
			measureName:      "Timer Resolution Requests " + msrName,
			measureSet:       socWatch,
			scenarioName:     meta.scenarioName,
			value:            0,
		}
	}
	for _, row := range records {
		// fmt.Printf("'%s': %s\n", row[colProcess], meta.browser)
		for _, browserProcessName := range meta.browserProcesses {
			if strings.HasPrefix(row[colProcess], browserProcessName) {
				for msrName, colId := range cols {
					val, err := strconv.ParseFloat(strings.Trim(row[colId], " "), 64)
					if err != nil {
						fmt.Printf("failed parse '%s': %v\n", row[colId], err)
						continue
					}
					mapMsrs[msrName].value += val
				}
			}
		}
	}

	for _, msr := range mapMsrs {
		if msr.value > 0 {
			msrs = append(msrs, *msr)
		}
	}

	return msrs
}
