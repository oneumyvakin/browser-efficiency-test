package main

import (
	"bufio"
	"fmt"
	"io"
	"os"
	"os/exec"
	"path/filepath"
	"regexp"
	"strconv"
	"strings"
)

const (
	amdProfCli                   = "amdProfCli"
	amdProfCliBinary             = `C:\Program Files\AMD\AMDuProf\bin\AMDuProfCLI.exe`
	symbolsYandex                = "SymbolsYandex"
	symbolsChromium              = "SymbolsChromium"
	symbolsServerMicrosoft       = "http://msdl.microsoft.com/download/symbols"
	symbolsServerChrome          = "https://chromium-browser-symsrv.commondatastorage.googleapis.com"
	symbolsCacheDirYabro         = "SymbolsCacheYabro"
	symbolsCacheDirChrome        = "SymbolsCacheChrome"
	symbolsCacheDirChromium      = "SymbolsCacheChromium"
	amdProfCliReportAllProcesses = "ALL PROCESSES (Sort Event - Energy)"
)

func generateChartsForAmdProfCliFiles(files []os.FileInfo) error {
	var measures []Measure

	for _, f := range files {
		if filepath.Ext(f.Name()) != ".pdata" {
			continue
		}

		resultDir := strings.TrimSuffix(f.Name(), filepath.Ext(f.Name()))
		if _, err := os.Stat(filepath.Join(*csvPath, amdProfCli, resultDir)); err == nil {
			// Report already exists, skip generating
			continue
		}

		pdataFilePath := filepath.Join(*csvPath, amdProfCli, f.Name())
		err := amdProfCliGenerateReport(pdataFilePath)
		if err != nil {
			fmt.Printf("%s: %v", pdataFilePath, err)
		}
	}

	for _, f := range files {
		if filepath.Ext(f.Name()) != ".pdata" {
			continue
		}

		subDir := strings.TrimSuffix(f.Name(), filepath.Ext(f.Name()))
		csvFilePath := filepath.Join(*csvPath, amdProfCli, subDir, subDir+".csv")

		m, err := amdProfCliGetMeasures(csvFilePath)
		if err != nil {
			return fmt.Errorf("%s: %v", csvFilePath, err)
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

func amdProfCliGenerateReport(pdataFilePath string) error {
	metaMeasure, err := generalGetFileMeta(pdataFilePath)
	if err != nil {
		return fmt.Errorf("failed to amdProfCliGenerateReport(%s): %s\n", pdataFilePath, err)
	}

	args := []string{
		"report",
		// "--cutoff", "10", // Limit reported processes to top 20 (Default: 10)
		"-i", pdataFilePath,
		"-o", filepath.Join(*csvPath, amdProfCli),
	}

	symbolArgs, err := getSymbolsArgs(metaMeasure.browserShortName)
	if err != nil {
		return fmt.Errorf("failed to amdProfCliGenerateReport(%s): %s\n", pdataFilePath, err)
	}

	args = append(
		args,
		symbolArgs...,
	)

	cmd := exec.Command(
		amdProfCliBinary,
		args...,
	)

	fmt.Printf("Execute: \"%s\" %s\n", amdProfCliBinary, args)

	err = cmd.Run()
	if err != nil {
		return fmt.Errorf("failed to amdProfCliGenerateReport(%s): %s\n", pdataFilePath, err)
	}

	return nil
}

func getSymbolsArgs(browserShortName string) ([]string, error) {
	if *withSymbols == false {
		return []string{}, nil
	}

	args := []string{}
	symbolsServer := symbolsServerMicrosoft

	v, err := getBrowserVersion(browserShortName)
	if err != nil {
		return []string{}, fmt.Errorf("failed to getBrowserVersion(%s): %s\n", browserShortName, err)
	}

	if (browserShortName == yaBrowserShortName || browserShortName == yaBrowserDefaultShortName) && v.version != "" {
		args = append(
			args,
			[]string{
				"--symbol-path", filepath.Join(symbolsYandex, "all_pdb-"+v.version),
				"--symbol-cache-dir", symbolsCacheDirYabro,
			}...,
		)
	}

	if browserShortName == chromiumShortName && v.chromiumVersion != "" {
		args = append(
			args,
			[]string{
				"--symbol-path", filepath.Join(symbolsChromium, v.chromiumVersion),
				"--symbol-cache-dir", symbolsCacheDirChromium,
			}...,
		)
	}

	if browserShortName == chromeShortName {
		symbolsServer = symbolsServer + ";" + symbolsServerChrome

		args = append(
			args,
			[]string{
				"--symbol-cache-dir", symbolsCacheDirChrome,
			}...,
		)
	}

	args = append(
		args,
		[]string{
			"--symbol-server", symbolsServer,
		}...,
	)

	return args, nil
}

func amdProfCliGetMeasures(csvFilePath string) ([]Measure, error) {
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
		amdProfCliReportAllProcesses: "",
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
			if line == fmt.Sprintf("%s\r\n", part) {
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
					dataline = strings.Replace(dataline, `"`, ``, -1)
					dataParts[part] += strings.Trim(dataline, " ")
				}
			}
		}
	}
	//fmt.Printf("%#v\n", dataParts)

	processParts := map[string]func([][]string, Measure) []Measure{
		amdProfCliReportAllProcesses: amdProfCliReportAllProcessesFunc,
	}

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

func amdProfCliReportAllProcessesFunc(records [][]string, meta Measure) []Measure {
	/*
		ALL PROCESSES (Sort Event - Energy)
		PROCESS,"Energy" (milli Joules),"CPU Time" (seconds)
		System Idle (PID - 0),12475.300,147.083
		c:\windows\system32\svchost.exe (PID - 1828),2135.332,3.532
		C:\Users\oneumyvakin\YandexDisk\Distros\yandex-18.6.0.742-66.0.3359.2\browser.exe (PID - 6688),267.393,0.428
		C:\WINDOWS\Explorer.EXE (PID - 4416),238.861,0.452
	*/
	//fmt.Printf("records: %v\n", records)

	var msrs []Measure
	colProcess := 0
	cols := map[string]int{
		"milli Joules": 1,
		"CPU Time":     2,
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
			measureSet:       amdProfCli,
			scenarioName:     meta.scenarioName,
			value:            0,
		}
	}
	for _, row := range records {
		//fmt.Printf("'%s': %s\n", row[colProcess], meta.browser)
		for _, browserProcessName := range meta.browserProcesses {
			re := regexp.MustCompile(browserProcessName)
			if re.MatchString(row[colProcess]) {
				for msrName, colId := range cols {
					val, err := strconv.ParseFloat(row[colId], 64)
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
