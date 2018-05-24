package main

import (
	"encoding/csv"
	"flag"
	"fmt"
	"github.com/pkg/errors"
	"github.com/wcharczuk/go-chart"
	"github.com/wcharczuk/go-chart/drawing"
	"io/ioutil"
	"math/big"
	"os"
	"path/filepath"
	"regexp"
	"sort"
	"strconv"
	"strings"
	"time"
)

const (
	csvColIdTest                    = 1
	csvColIdIteration               = 2
	csvColIdMeasureSet              = 6
	csvColIdMeasure                 = 7
	csvColIdResult                  = 8
	yaBrowserShortName              = "yabro"
	yaBrowserProcessName            = "browser.exe"
	yaBrowserDefaultShortName       = "brodefault"
	yaBrowserDefaultProcessName     = "brodefault.exe"
	chromeShortName                 = "chrome"
	chromeProcessName               = "chrome.exe"
	chromiumShortName               = "chromium"
	chromiumProcessName             = "chromium.exe"
	operaShortName                  = "opera"
	operaProcessName                = "opera.exe"
	firefoxShortName                = "firefox"
	firefoxProcessName              = "firefox.exe"
	microsoftEdgeShortName          = "edge"
	microsoftEdgeProcessName        = "MicrosoftEdge.exe"
	microsoftEdgeContentProcessName = "MicrosoftEdgeCP.exe"
)

var (
	chartDate    time.Time
	csvPath      *string
	pngPath      *string
	cmpIn1       *string
	cmpIn2       *string
	cmpOut       *string
	withSymbols  *bool
	processNames = map[string]bool{
		yaBrowserProcessName: true, yaBrowserDefaultProcessName: true,
		chromeProcessName:   true,
		chromiumProcessName: true,
		operaProcessName:    true,
		firefoxProcessName:  true, microsoftEdgeProcessName: true, microsoftEdgeContentProcessName: true,
	}
	processNameChartColor = map[string]drawing.Color{
		yaBrowserProcessName:            drawing.ColorRed,
		yaBrowserDefaultProcessName:     drawing.ColorFromHex("e094a9"),
		chromeProcessName:               drawing.ColorBlue,
		chromiumProcessName:             drawing.ColorFromHex("3ca6c5"),
		operaProcessName:                drawing.ColorBlack,
		firefoxProcessName:              drawing.ColorFromHex("ff6600"),
		microsoftEdgeProcessName:        drawing.ColorFromHex("0b6097"),
		microsoftEdgeContentProcessName: drawing.ColorFromHex("0b6097"),
	}
	browserNameToProcessName = map[string]string{
		yaBrowserShortName:        yaBrowserProcessName,
		yaBrowserDefaultShortName: yaBrowserDefaultProcessName,
		chromeShortName:           chromeProcessName,
		chromiumShortName:         chromiumProcessName,
		operaShortName:            operaProcessName,
		firefoxShortName:          firefoxProcessName,
		microsoftEdgeShortName:    microsoftEdgeProcessName,
	}
	browserShortNameToProcesses = map[string][]string{
		yaBrowserShortName:        {yaBrowserProcessName},
		yaBrowserDefaultShortName: {yaBrowserDefaultProcessName},
		chromeShortName:           {chromeProcessName},
		chromiumShortName:         {chromiumProcessName},
		operaShortName:            {operaProcessName},
		firefoxShortName:          {firefoxProcessName},
		microsoftEdgeShortName:    {microsoftEdgeProcessName, microsoftEdgeContentProcessName},
	}
	browserShortNameToUserDataTmp = map[string]string{
		yaBrowserShortName:        "yabroUserDataTmp",
		yaBrowserDefaultShortName: "brodefaultUserDataTmp",
		chromeShortName:           "chromeUserDataTmp",
		chromiumShortName:         "chromiumUserDataTmp",
		operaShortName:            "operaUserDataTmp",
	}
)

type Measure struct {
	iteration        string
	measureSet       string
	measureName      string
	scenarioName     string
	browser          string
	browserShortName string
	browserProcesses []string
	date             time.Time
	value            float64
}

func init() {
	chartDate = time.Now()
	csvPath = flag.String("csv", "", "Path to directory with PerformanceResults_nnnn_nnnn.csv")
	pngPath = flag.String("png", "", "Path to output directory for PNG files")
	withSymbols = flag.Bool("withSymbols", false, "Process data with symbols paths")
	// comparing
	cmpIn1 = flag.String("cmpIn1", "", "Path to first directory for comparing")
	cmpIn2 = flag.String("cmpIn2", "", "Path to second directory for comparing")
	cmpOut = flag.String("cmpOut", "", "Path to output directory for result of comparing")
}

func main() {
	flag.Parse()

	if *cmpIn1 != "" && *cmpIn2 != "" && *cmpOut != "" {
		if err := mergePng(*cmpIn1, *cmpIn2, *cmpOut); err != nil {
			fmt.Printf("failed mergePng: %s", err)
		}
		return
	}

	if *csvPath == "" {
		fmt.Println("-csv arg is empty")
		return
	}
	if *pngPath == "" {
		*pngPath = *csvPath
	}
	files, err := ioutil.ReadDir(*csvPath)
	if err != nil {
		fmt.Printf("csv readdir %s err %s", *csvPath, err)
		return
	}

	err = generateChartsForPerformanceCsv(files)
	if err != nil {
		fmt.Printf("generateChartsForPerformanceCsv err %v\n", err)
		return
	}

	err = generateChartsForIntelPowerLogFiles(files)
	if err != nil {
		fmt.Printf("generateChartsForIntelPowerLogFiles err %v\n", err)
		return
	}

	err = generateChartsForYandexBenchmarkFiles(files)
	if err != nil {
		fmt.Printf("generateChartsForYandexBenchmarkFiles err %v\n", err)
		return
	}

	err = generateChartsForSrumFiles(files)
	if err != nil {
		fmt.Printf("generateChartsForSrumFiles err %v\n", err)
		return
	}

	err = generateChartsForProcmonFiles(files)
	if err != nil {
		fmt.Printf("generateChartsForProcmonFiles err %v\n", err)
		return
	}

	err = generateChartsForIppetFiles(files)
	if err != nil {
		fmt.Printf("generateChartsForIppetFiles err %v\n", err)
		return
	}

	if _, err := os.Stat(filepath.Join(*csvPath, socWatch)); err == nil {
		files, err = ioutil.ReadDir(filepath.Join(*csvPath, socWatch))
		if err != nil {
			fmt.Printf("readdir %s: %v\n", filepath.Join(*csvPath, socWatch), err)
			return
		}
		err = generateChartsForSocWatchFiles(files)
		if err != nil {
			fmt.Printf("generateChartsForSocWatchFiles:\n%v\n", err)
			return
		}
	}

	if _, err := os.Stat(filepath.Join(*csvPath, amdProfCli)); err == nil {
		files, err = ioutil.ReadDir(filepath.Join(*csvPath, amdProfCli))
		if err != nil {
			fmt.Printf("readdir %s: %v\n", filepath.Join(*csvPath, amdProfCli), err)
			return
		}
		err = generateChartsForAmdProfCliFiles(files)
		if err != nil {
			fmt.Printf("generateChartsForAmdProfCliFiles:\n%v\n", err)
			return
		}
	}
}

func generalGetFileMeta(csvFilePath string) (Measure, error) {
	base := filepath.Base(csvFilePath)
	fileMetaTokens := strings.Split(base, "_")
	m := Measure{}
	if len(fileMetaTokens) < 6 {
		return m, fmt.Errorf("generalGetFileMeta not enough tokens in '%s'", csvFilePath)
	}
	m.browser = browserNameToProcessName[fileMetaTokens[0]]
	m.browserShortName = fileMetaTokens[0]
	m.browserProcesses = browserShortNameToProcesses[m.browserShortName]
	m.scenarioName = fileMetaTokens[1]
	m.iteration = fileMetaTokens[2]
	m.measureSet = fileMetaTokens[3]
	var err error
	// 20180120_224843-1.xml
	m.date, err = time.Parse("20060102 150405", fileMetaTokens[4]+" "+fileMetaTokens[5][0:6])
	return m, err
}

func generateChartsForPerformanceCsv(files []os.FileInfo) error {
	for _, f := range files {
		//fmt.Println(f.Name(), filepath.Ext(f.Name()))
		if filepath.Ext(f.Name()) != ".csv" {
			continue
		}
		if strings.HasPrefix(f.Name(), "Performance") {
			err := processCsvFile(filepath.Join(*csvPath, f.Name()))
			if err != nil {
				return err
			}
		}
	}

	return nil
}

func processCsvFile(csvFilePath string) error {
	csvFile, err := os.OpenFile(csvFilePath, os.O_RDONLY, 0666)
	if err != nil {
		return fmt.Errorf("Open CSV file %s err %v\n", csvFilePath, err)
	}

	records, err := getRecordsFromCsvFile(csvFile, ',')
	if err != nil {
		fmt.Printf("getRecordsFromCsvFile %s err %v\n", csvFilePath, err)
		return fmt.Errorf("getRecordsFromCsvFile %s err %v\n", csvFilePath, err)
	}

	chartBars := getChartBarsFromRawResults(groupCsvRecordsByMeasureSet(records), 2)

	for measureSet, barValues := range chartBars {
		err := drawBars(measureSet, barValues)
		if err != nil {
			return err
		}
	}

	groupedBars := getIterationsBarsFromGroupedMeasures(groupCsvRecordsByIteration(records), 2)
	for measureSet, barValues := range groupedBars {
		err := drawBars(measureSet, barValues)
		if err != nil {
			return err
		}
	}

	return nil
}

func getChartBarsFromRawResults(raw map[string]map[string][]float64, precision int) map[string][]chart.Value {
	chartBars := make(map[string][]chart.Value)
	for setName, setResults := range raw {
		for browserName, resultList := range setResults {
			barValue := median(resultList)
			value := chart.Value{
				Label: fmt.Sprintf("%s (%s)", browserName, big.NewFloat(barValue).Text('f', precision)),
				Value: barValue,
				Style: chart.Style{
					Show:        true,
					FillColor:   processNameChartColor[browserName],
					StrokeColor: processNameChartColor[browserName],
				},
			}
			chartBars[setName] = append(chartBars[setName], value)
			sort.Sort(chartValueSortByBrowser(chartBars[setName]))
		}
	}

	return appendDiffBar(chartBars, precision)
}

func appendDiffBar(bars map[string][]chart.Value, precision int) map[string][]chart.Value {
	for setName, setResults := range bars {
		if len(setResults) > 1 {
			bars[setName] = append(bars[setName], getDiffBars(setName, setResults, precision)...)
		}
	}

	return bars
}

const (
	smallerIsBetter       = true
	biggerIsBetter        = false
	smallerIsBetterString = "smaller is better"
	biggerIsBetterString  = "bigger is better"
)

var (
	excludedFromBad = map[string]bool{
		"YandexBenchmarkJetStream":   true,
		"YandexBenchmarkMotionMark":  true,
		"YandexBenchmarkSpeedometer": true,
	}
	smallerBiggerExplain = map[bool]string{
		true:  smallerIsBetterString,
		false: biggerIsBetterString,
	}
	diffBarColor = map[string]drawing.Color{
		"Good": drawing.ColorFromHex("4ce600"),
		"Bad":  drawing.Color{R: 110, G: 128, B: 139, A: 255},
	}
	diffBarStrokeColor = map[string]drawing.Color{
		"Good": drawing.ColorFromHex("eeffe6"),
		"Bad":  drawing.Color{R: 110, G: 128, B: 139, A: 255},
	}
)

func getDiffBars(setName string, bars []chart.Value, precision int) []chart.Value {
	diffBars := []chart.Value{}

	yaBrowserBar := chart.Value{}
	competitorBars := []chart.Value{}
	for _, bar := range bars {
		if strings.Contains(bar.Label, yaBrowserProcessName) {
			yaBrowserBar = bar
			continue
		}

		competitorBars = append(competitorBars, bar)
	}
	//fmt.Println(setName)
	for _, competitorBar := range competitorBars {
		diffBarValue := competitorBar.Value - yaBrowserBar.Value

		diffBar := chart.Value{}
		diffBar.Value = diffBarValue

		diffKind := "Good"
		diffKindExplain := smallerBiggerExplain[smallerIsBetter]
		diffKindSign := float64(1)
		if diffBar.Value < 0 {
			diffKind = "Bad"
			if excludedFromBad[setName] {
				diffKind = "Good"
				diffKindExplain = smallerBiggerExplain[biggerIsBetter]
			}
			diffKindSign = float64(-1)
			diffBar.Value = diffBar.Value * diffKindSign
		} else {
			if excludedFromBad[setName] {
				diffKind = "Bad"
				diffKindExplain = smallerBiggerExplain[biggerIsBetter]
			}
		}

		var competitorProcessName string
		for _, competitor := range browserNameToProcessName {
			if strings.Contains(competitorBar.Label, competitor) {
				competitorProcessName = competitor
				break
			}
		}

		diffBar.Label = fmt.Sprintf(
			"%s vs %s (%s) diff %.0f%% (%s)",
			diffKind,
			competitorProcessName,
			diffKindExplain,
			diffBarValue*100/competitorBar.Value*diffKindSign,
			big.NewFloat(diffBar.Value).Text('f', precision),
		)
		diffBar.Style.FillColor = diffBarColor[diffKind]
		diffBar.Style.StrokeColor = diffBarStrokeColor[diffKind]
		diffBars = append(diffBars, diffBar)
	}

	return diffBars
}

func getIterationsBarsFromGroupedMeasures(raw map[string]map[string][]Measure, precision int) map[string][]chart.Value {
	chartBars := make(map[string][]chart.Value)
	for setName, setResults := range raw {
		for iteration, measures := range setResults {
			for _, measure := range measures {
				value := chart.Value{
					Label: fmt.Sprintf("%s: %s (%s) ", iteration, measure.browser, big.NewFloat(measure.value).Text('f', precision)),
					Value: measure.value,
					Style: chart.Style{
						Show:        true,
						FillColor:   processNameChartColor[measure.browser],
						StrokeColor: processNameChartColor[measure.browser],
					},
				}
				chartBars[setName] = append(chartBars[setName], value)
			}
		}
	}

	for setName, setValues := range chartBars {
		sorted := ChartValuesSortedByLabel{
			values: setValues,
		}
		sorted.Sort()

		chartBars[setName] = sorted.values
	}
	return chartBars
}

func groupCsvRecordsByIteration(records [][]string) map[string]map[string][]Measure {
	//fmt.Printf("records: %#v\n", records)

	browserResults := map[string]map[string][]Measure{}

	// "gpuUsage GPU time  (us)" > 0 > [Measure0, Measure1]
	for _, row := range records {
		for browserName := range processNames {
			if strings.Contains(row[csvColIdMeasure], browserName) {
				iteration := row[csvColIdIteration]
				measureName := row[csvColIdMeasure]
				fullSetName := getMeasureSetFullName(
					row[csvColIdMeasureSet], browserName, measureName, row[csvColIdTest]) + " by iterations"

				if browserResults[fullSetName] == nil {
					browserResults[fullSetName] = make(map[string][]Measure)
				}
				measureValue, err := strconv.ParseFloat(strings.Replace(row[csvColIdResult], ",", ".", 1), 64)
				if err != nil {
					fmt.Printf("ParseFloat(%s) err %s\n", row[csvColIdResult], err)
					continue
				}

				m := Measure{
					iteration:   iteration,
					measureSet:  fullSetName,
					measureName: measureName,
					browser:     browserName,
					value:       measureValue,
				}
				browserResults[fullSetName][iteration] = append(browserResults[fullSetName][iteration], m)
			}
		}
	}

	//fmt.Printf("Results: %#v\n", browserResults)
	return browserResults
}

func groupCsvRecordsByMeasureSet(records [][]string) map[string]map[string][]float64 {
	//fmt.Printf("records: %#v\n", records)

	browserResults := map[string]map[string][]float64{}

	// "gpuUsage GPU time  (us)" > "browser.exe" : [1.23, 3.45, 5,67]
	//                           > "chrome.exe"  : [1.23, 3.45, 5,67]
	for _, row := range records {
		for browserName := range processNames {
			if strings.Contains(row[csvColIdMeasure], browserName) {
				fullSetName := getMeasureSetFullName(
					row[csvColIdMeasureSet], browserName, row[csvColIdMeasure], row[csvColIdTest],
				)

				if browserResults[fullSetName] == nil {
					browserResults[fullSetName] = make(map[string][]float64)
				}
				measureResultFloat64, err := strconv.ParseFloat(strings.Replace(row[csvColIdResult], ",", ".", 1), 64)
				if err != nil {
					fmt.Printf("ParseFloat(%s) err %s\n", row[csvColIdResult], err)
					continue
				}

				browserResults[fullSetName][browserName] = append(browserResults[fullSetName][browserName], measureResultFloat64)
			}
		}
	}

	//fmt.Printf("Results: %#v\n", browserResults)
	return browserResults
}

func groupMeasuresBySet(measures []Measure) map[string]map[string][]float64 {
	browserResults := map[string]map[string][]float64{}
	for _, m := range measures {
		browserProcessName := m.browser
		fullSetName := getMeasureSetFullName(
			m.measureSet, browserProcessName, m.measureName, m.scenarioName,
		)

		if browserResults[fullSetName] == nil {
			browserResults[fullSetName] = make(map[string][]float64)
		}

		browserResults[fullSetName][browserProcessName] = append(browserResults[fullSetName][browserProcessName], m.value)
	}

	//fmt.Printf("%#v\n", browserResults)
	return browserResults
}

// measureSetName like "gpuUsage"
// measureName like "GPU time browser.exe (us)"
// testName like "yandexyarunewtab"
//
// returns "gpuUsage GPU time  (us) yandexyarunewtab"
func getMeasureSetFullName(measureSetName, browserName, measureName, testName string) string {
	return strings.Trim(fmt.Sprintf(
		"%s %s %s",
		measureSetName,
		strings.Replace(measureName, browserName, "", 1),
		testName), " ")
}

func getRecordsFromCsvFile(csvFile *os.File, sep rune) ([][]string, error) {
	r := csv.NewReader(csvFile)
	r.Comma = sep
	records, err := r.ReadAll()
	if err != nil {
		return nil, err
	}

	return records, nil
}

func getRecordsFromString(s string, sep rune) ([][]string, error) {
	r := csv.NewReader(strings.NewReader(s))
	r.Comma = sep
	records, err := r.ReadAll()
	if err != nil {
		return nil, err
	}

	return records, nil
}

type barDrawer func(*os.File, string, []chart.Value)

// measureSet like "CPU  Utilization %" or "GPU Time  (us)"
func drawBars(measureSet string, chartBars []chart.Value) error {
	drawers := map[string]barDrawer{
		srumMeasureSet:                drawBarGpuTime,
		yandexBenchmark:               drawBarGpuTime,
		intelPowerMeasureSet:          drawBarGpuTime,
		socWatch:                      drawBarGpuTime,
		amdProfCli:                    drawBarGpuTime,
		"ippet":                       drawBarGpuTime,
		"diskIo Disk IO Time":         drawBarGpuTime,
		"diskIo Disk IO Size":         drawBarGpuTime,
		"diskIo Disk Service Time":    drawBarGpuTime,
		"fileIo File IO Duration":     drawBarGpuTime,
		"fileIo File IO Size":         drawBarGpuTime,
		"memSet WorkingSet":           drawBarGpuTime,
		"memSet PrivateWorkingSet":    drawBarGpuTime,
		"memSet VirtualSize":          drawBarGpuTime,
		"gpuUsage GPU Time":           drawBarGpuTime,
		"gpuUsage GPU Packets":        drawBarGpuTime,
		"gpuUsage GPU Percentage":     drawBarCpuPercentage,
		"cpuUsage CPU  Utilization %": drawBarCpuPercentage,
	}

	for drawerId, drawerFunc := range drawers {
		//fmt.Println("drawers loop", drawerId, measureSet)
		if strings.HasPrefix(measureSet, drawerId) {
			//fmt.Println("drawers loop matched", drawerId, measureSet)
			pngFile, err := os.Create(filepath.Join(*pngPath, measureSet+".png"))
			if err != nil {
				fmt.Printf("Create PNG file %s err %v\n", *pngPath, err)
				return fmt.Errorf("Create PNG file %s err %v\n", *pngPath, err)
			}
			//fmt.Printf("Bars: \n%#v\n", chartBars)
			drawerFunc(pngFile, measureSet, chartBars)
		}
	}

	return nil
}

type chartValueSortByBrowser []chart.Value

func (s chartValueSortByBrowser) Len() int {
	return len(s)
}
func (s chartValueSortByBrowser) Swap(i, j int) {
	s[i], s[j] = s[j], s[i]
}
func (s chartValueSortByBrowser) Less(i, j int) bool {
	return s[i].Label < s[j].Label
}

func drawBarGpuTime(fileWriter *os.File, measureSet string, chartBars []chart.Value) {
	barWidth := 60
	chartWidth := 1024
	fontSize := 10.0
	if len(chartBars) > 9 {
		barWidth = 40
		chartWidth += (len(chartBars) - 9) * 100
		fontSize = 8.0
	}

	max := 0.0
	for _, bar := range chartBars {
		if bar.Value > max {
			max = bar.Value
		}
	}

	sbc := chart.BarChart{
		Background: chart.Style{
			Padding: chart.Box{
				Top: 40,
			},
		},
		Title:      fmt.Sprintf("%s %s", measureSet, chartDate.Format("2006-01-02 15:04:05")),
		TitleStyle: chart.StyleShow(),
		Width:      chartWidth,
		//Height:   512,
		BarWidth: barWidth,
		XAxis: chart.Style{
			Show:     true,
			FontSize: fontSize,
		},
		YAxis: chart.YAxis{
			Style: chart.Style{
				Show: true,
			},
			Range: &chart.ContinuousRange{
				Min: 0,
				Max: max,
			},
		},
		Bars: chartBars,
	}

	err := sbc.Render(chart.PNG, fileWriter)
	if err != nil {
		fmt.Printf("Error rendering chart: %v\n", err)
	}
}

func drawBarCpuPercentage(fileWriter *os.File, measureSet string, chartBars []chart.Value) {
	barWidth := 60
	chartWidth := 1024
	fontSize := 10.0
	if len(chartBars) > 9 {
		barWidth = 40
		chartWidth += (len(chartBars) - 9) * 100
		fontSize = 8.0
	}

	sbc := chart.BarChart{
		Title:      measureSet,
		TitleStyle: chart.StyleShow(),
		Width:      chartWidth,
		//Height:   512,
		BarWidth: barWidth,
		XAxis: chart.Style{
			Show:     true,
			FontSize: fontSize,
		},
		YAxis: chart.YAxis{
			Style: chart.Style{
				Show: true,
			},
			Range: &chart.ContinuousRange{
				Min: 0,
				Max: 100,
			},
		},
		Bars: chartBars,
	}

	err := sbc.Render(chart.PNG, fileWriter)
	if err != nil {
		fmt.Printf("Error rendering chart: %v\n", err)
	}
}

func median(numbers []float64) float64 {
	sort.Float64s(numbers)
	middle := len(numbers) / 2
	result := numbers[middle]
	if len(numbers)%2 == 0 {
		result = (result + numbers[middle-1]) / 2
	}
	return result
}

type ChartValuesSortedByLabel struct {
	values []chart.Value
}

func (v *ChartValuesSortedByLabel) Sort() {
	sort.Sort(v)
}

func (v ChartValuesSortedByLabel) Len() int {
	return len(v.values)
}

func (v ChartValuesSortedByLabel) Less(i, j int) bool {
	if v.values[i].Label < v.values[j].Label {
		return true
	}
	return false
}

func (v ChartValuesSortedByLabel) Swap(i, j int) {
	v.values[i], v.values[j] = v.values[j], v.values[i]
}

type version struct {
	version         string
	chromiumVersion string
}

func getBrowserVersion(browserShortName string) (version, error) {
	v := version{}
	if browserShortName == "" {
		return v, errors.New("empty browserShortName")
	}
	if tmpFolder, found := browserShortNameToUserDataTmp[browserShortName]; found {
		return getBrowserVersionFromTmpFolder(tmpFolder)
	}

	return v, nil
}

func getBrowserVersionFromTmpFolder(tmpFolder string) (version, error) {
	v := version{}
	localState := "Local State"
	lsContent, err := ioutil.ReadFile(filepath.Join(tmpFolder, localState))
	if err != nil {
		return v, nil
	}

	// "last_runned_version":"18.7.0.85"
	// "last_startup_version":"65.0.3325.181"
	reBro := regexp.MustCompile(`"last_runned_version":"(\d\d\.\d+\.\d+.\d+)"`)
	broSubmatch := reBro.FindSubmatch(lsContent)
	if len(broSubmatch) > 0 {
		v.version = string(broSubmatch[1])
	}
	reChromium := regexp.MustCompile(`"last_startup_version":"(\d+\.\d+\.\d+.\d+)"`)
	v.chromiumVersion = string(reChromium.FindSubmatch(lsContent)[1])

	return v, nil
}
