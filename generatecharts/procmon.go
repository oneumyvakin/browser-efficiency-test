package main

import (
	"encoding/json"
	"encoding/xml"
	"fmt"
	"html/template"
	"io/ioutil"
	"os"
	"os/exec"
	"path/filepath"
	"regexp"
	"sort"
	"strconv"
	"strings"
	"time"
)

type procmon struct {
	EventList procmonEventList `xml:"eventlist"`
}

type procmonEventList struct {
	Events []procmonEvent `xml:"event"`
}

type procmonEvent struct {
	ProcessIndex int     `xml:"ProcessIndex"`
	Time         string  `xml:"Time_of_Day"`
	ProcessName  string  `xml:"Process_Name"`
	PID          int     `xml:"PID"`
	Operation    string  `xml:"Operation"`
	Path         string  `xml:"Path"`
	Result       string  `xml:"Result"`
	Detail       string  `xml:"Detail"`
	Duration     float64 `xml:"Duration"`
	Category     string  `xml:"Category"`
	RelativeTime string  `xml:"Relative_Time"`
	TID          int     `xml:"TID"`
	ParentPID    int     `xml:"Parent_PID"`
}

type OperationName string

type OperationParam string

type RelativeTime time.Time

func (s RelativeTime) String() string {
	return time.Time(s).Format("15:04:05")
}

type procmonFileStats struct {
	PID             map[int]RelativeTimeSortedList           `json:"-"`
	TID             map[int]RelativeTimeSortedList           `json:"-"`
	ParentPID       map[int]RelativeTimeSortedList           `json:"-"`
	RelativeTime    RelativeTimeSortedList                   `json:"-"`
	RelativeTimeMin RelativeTime                             `json:"RelativeTimeMin"`
	RelativeTimeMax RelativeTime                             `json:"RelativeTimeMax"`
	TotalDuration   int                                      `json:"TotalDuration"`
	Duration        int                                      `json:"Duration"`
	TotalLength     int                                      `json:"TotalLength"`
	Length          int                                      `json:"Length"`
	TotalCount      int                                      `json:"TotalCount"`
	Count           int                                      `json:"Count"`
	PercentDuration int                                      `json:"PercentDuration"`
	PercentLength   int                                      `json:"PercentLength"`
	PercentCount    int                                      `json:"PercentCount"`
	Path            string                                   `json:"Path"`
	Operation       map[OperationName]map[OperationParam]int `json:"Operation"`
}

type procmonFileStatsSortByCount []procmonFileStats

func (s procmonFileStatsSortByCount) Len() int {
	return len(s)
}
func (s procmonFileStatsSortByCount) Swap(i, j int) {
	s[i], s[j] = s[j], s[i]
}
func (s procmonFileStatsSortByCount) Less(i, j int) bool {
	return s[i].Count < s[j].Count
}

type procmonFileStatsSortByLength []procmonFileStats

func (s procmonFileStatsSortByLength) Len() int {
	return len(s)
}
func (s procmonFileStatsSortByLength) Swap(i, j int) {
	s[i], s[j] = s[j], s[i]
}
func (s procmonFileStatsSortByLength) Less(i, j int) bool {
	return s[i].Length < s[j].Length
}

type procmonFileStatsSortByDuration []procmonFileStats

func (s procmonFileStatsSortByDuration) Len() int {
	return len(s)
}
func (s procmonFileStatsSortByDuration) Swap(i, j int) {
	s[i], s[j] = s[j], s[i]
}
func (s procmonFileStatsSortByDuration) Less(i, j int) bool {
	return s[i].Length < s[j].Length
}

type RelativeTimeSortedList []RelativeTime

func (s RelativeTimeSortedList) String() string {
	if len(s) > 0 {
		return fmt.Sprintf("Count: %d, From %s to %s", len(s), s[0], s[len(s)-1])
	}
	return ""
}

func (s RelativeTimeSortedList) Len() int {
	return len(s)
}
func (s RelativeTimeSortedList) Swap(i, j int) {
	s[i], s[j] = s[j], s[i]
}
func (s RelativeTimeSortedList) Less(i, j int) bool {
	return time.Time(s[i]).Before(time.Time(s[j]))
}

func generateChartsForProcmonFiles(files []os.FileInfo) error {
	pmlBrowserProcessed := map[string][]string{} // browserShortName to pml file names

	for _, f := range files {
		if filepath.Ext(f.Name()) != ".pml" {
			continue
		}

		meta, err := generalGetFileMeta(f.Name())
		if err != nil {
			return err
		}
		pmlBrowserProcessed[meta.browserShortName] = append(pmlBrowserProcessed[meta.browserShortName], f.Name())
	}

	//fmt.Printf("%#v\n", pmlBrowserProcessed)

	pmlFinalList := []string{}
	for browserShortName := range pmlBrowserProcessed {
		sort.Strings(pmlBrowserProcessed[browserShortName])
		//fmt.Printf("%#v\n", pmlBrowserProcessed[browserShortName])
		pmlFinalList = append(pmlFinalList, pmlBrowserProcessed[browserShortName][len(pmlBrowserProcessed[browserShortName])-1])
	}
	//fmt.Printf("%#v\n", pmlFinalList)

	for _, fName := range pmlFinalList {
		//fmt.Println(f.Name(), filepath.Ext(f.Name()))

		xmlFileName := strings.Replace(fName, ".pml", ".xml", 1)
		xmlExportPath := filepath.Join(*csvPath, xmlFileName)
		if _, err := os.Stat(xmlExportPath); err == nil {
			continue
		}
		pmlFilePath := filepath.Join(*csvPath, fName)

		err := procmonExport(pmlFilePath, xmlExportPath)
		if err != nil {
			fmt.Printf("failed to procmonExport %s: %s", pmlFilePath, err)
			return err
		}
	}

	files, err := ioutil.ReadDir(*csvPath)
	if err != nil {
		fmt.Printf("failed to read dir for XML files %s: %s", *csvPath, err)
		return err
	}

	//Offset: 0, Length: 45 741, I/O Flags: Non-cached, Paging I/O, Priority: Normal
	//Offset: 16, Length: 8 192
	var lengthRegExp = []*regexp.Regexp{
		regexp.MustCompile(`Length: (.+\d),`),
		regexp.MustCompile(`Length: (.+\d)$`),
	}

	// iteration > browser.exe > c:\path
	procmonStats := map[string]map[string]map[string]procmonFileStats{}
	for _, f := range files {
		if filepath.Ext(f.Name()) != ".xml" {
			continue
		}

		if !strings.Contains(f.Name(), "_procmon_") {
			continue
		}

		xmlFileName := filepath.Join(*csvPath, f.Name())

		xmlFile, err := os.Open(xmlFileName)
		if err != nil {
			if xmlFile != nil {
				xmlFile.Close()
			}
			return err
		}
		var p procmon
		err = xml.NewDecoder(xmlFile).Decode(&p)
		if err != nil {
			fmt.Printf("failed to decode %s: %s", xmlFileName, err)
			return err
		}

		meta, err := generalGetFileMeta(xmlFileName)
		if err != nil {
			return err
		}

		for _, event := range p.EventList.Events {
			if procmonStats[meta.iteration] == nil {
				procmonStats[meta.iteration] = map[string]map[string]procmonFileStats{}
			}

			if procmonStats[meta.iteration][event.ProcessName] == nil {
				procmonStats[meta.iteration][event.ProcessName] = map[string]procmonFileStats{}
			}

			if _, exists := procmonStats[meta.iteration][event.ProcessName][event.Path]; !exists {
				procmonStats[meta.iteration][event.ProcessName][event.Path] = procmonFileStats{
					PID:       map[int]RelativeTimeSortedList{},
					TID:       map[int]RelativeTimeSortedList{},
					ParentPID: map[int]RelativeTimeSortedList{},
					Operation: map[OperationName]map[OperationParam]int{},
				}
			}
			tmp := procmonStats[meta.iteration][event.ProcessName][event.Path]
			tmp.Count++
			tmp.Path = event.Path
			tmp.Duration += int(event.Duration * 10000000)
			relativeTime, err := time.Parse("15:04:05.0000000", event.RelativeTime)
			if err != nil {
				return err
			}
			tmp.RelativeTime = append(tmp.PID[event.PID], RelativeTime(relativeTime))
			tmp.PID[event.PID] = append(tmp.PID[event.PID], RelativeTime(relativeTime))
			tmp.PID[event.TID] = append(tmp.PID[event.TID], RelativeTime(relativeTime))
			tmp.PID[event.ParentPID] = append(tmp.ParentPID[event.ParentPID], RelativeTime(relativeTime))
			procmonStats[meta.iteration][event.ProcessName][event.Path] = tmp

			if procmonStats[meta.iteration][event.ProcessName][event.Path].Operation[OperationName(event.Operation)] == nil {
				procmonStats[meta.iteration][event.ProcessName][event.Path].Operation[OperationName(event.Operation)] = map[OperationParam]int{}
			}
			procmonStats[meta.iteration][event.ProcessName][event.Path].Operation[OperationName(event.Operation)][OperationParam("Count")]++
			procmonStats[meta.iteration][event.ProcessName][event.Path].Operation[OperationName(event.Operation)][OperationParam("Duration")] += int(event.Duration * 10000000)

			if event.Operation == "UnlockFileSingle" || event.Operation == "LockFile" {
				continue // Do not calculate Length for Lock/Unlock Operations
			}
			for _, re := range lengthRegExp {
				match := re.FindStringSubmatch(event.Detail)
				//fmt.Println(match)
				if len(match) > 1 {
					lengthVal := strings.Replace(match[1], "\u00a0", "", -1)
					lengthVal = strings.Replace(lengthVal, ",", "", -1)
					lengthVal = strings.Replace(lengthVal, " ", "", -1)
					length, err := strconv.Atoi(lengthVal)
					if err != nil {
						fmt.Println(err)
						continue
					}
					tmp := procmonStats[meta.iteration][event.ProcessName][event.Path]
					tmp.Length += length
					procmonStats[meta.iteration][event.ProcessName][event.Path] = tmp
					procmonStats[meta.iteration][event.ProcessName][event.Path].Operation[OperationName(event.Operation)][OperationParam("Length")] += length
				}
			}
		}

		if xmlFile != nil {
			xmlFile.Close()
		}
	}

	//fmt.Printf("%s\n", procmonStats)
	for iteration, iterationStats := range procmonStats {
		for processName, stats := range iterationStats {
			totalDuration := 0
			totalLength := 0
			totalCount := 0
			for _, stat := range stats {
				totalDuration += stat.Duration
				totalLength += stat.Length
				totalCount += stat.Count
			}

			for path, stat := range stats {
				stat.TotalDuration = totalDuration
				stat.TotalLength = totalLength
				stat.TotalCount = totalCount
				stat.PercentDuration = stat.Duration * 100 / totalDuration
				stat.PercentLength = stat.Length * 100 / totalLength
				stat.PercentCount = stat.Count * 100 / totalCount
				sort.Sort(RelativeTimeSortedList(stat.RelativeTime))
				if len(stat.RelativeTime) > 0 {
					stat.RelativeTimeMin = stat.RelativeTime[0]
					stat.RelativeTimeMax = stat.RelativeTime[len(stat.RelativeTime)-1]
				}

				for PID := range stat.PID {
					sort.Sort(RelativeTimeSortedList(stat.PID[PID]))
				}
				for TID := range stat.TID {
					sort.Sort(RelativeTimeSortedList(stat.TID[TID]))
				}
				for ParentPID := range stat.ParentPID {
					sort.Sort(RelativeTimeSortedList(stat.ParentPID[ParentPID]))
				}
				sort.Sort(RelativeTimeSortedList(stat.RelativeTime))
				procmonStats[iteration][processName][path] = stat
			}
		}
	}

	for iteration, iterationStats := range procmonStats {
		procmonReportJsonFileName := fmt.Sprintf("procmonFileStat-%s.json", iteration)
		procmonReportJsonFile, err := os.OpenFile(filepath.Join(*csvPath, procmonReportJsonFileName), os.O_CREATE|os.O_TRUNC, 0666)
		if err != nil {
			if procmonReportJsonFile != nil {
				procmonReportJsonFile.Close()
			}
			return err
		}
		defer procmonReportJsonFile.Close()

		err = json.NewEncoder(procmonReportJsonFile).Encode(iterationStats)
		if err != nil {
			fmt.Printf("failed to json encode %s: %s\n", procmonReportJsonFileName, err)
			return err
		}
	}

	err = procmonGenerateReportTopN(procmonStats)
	if err != nil {
		fmt.Printf("failed to procmonGenerateHtmlReport: %s\n", err)
		return err
	}

	return nil
}

const procmonReportTplBaseTopN = `
<!DOCTYPE html>
<html>
	<head>
		<meta charset="UTF-8">
		<title></title>
		<style>
			.wrapper {
				display: grid;
				grid-template-columns: 410px 410px 410px;
				grid-template-rows: 320px 320px;
			}
			.box {
				overflow: auto;
			}
		</style>
<script>
function showHide(el) {
    if (el.style.display === "none") {
        el.style.display = "block";
    } else {
        el.style.display = "none";
    }
} 
</script>
	</head>
	<body>
		<div class="wrapper">
		{{range $browser, $stats := . }}
			<div class="box">{{ $browser }}
				{{ range $i, $stat := $stats }}
					<br> 
					<b>{{ $i }}. </b> <a href="#" onclick="showHide(this.nextElementSibling)">{{ $stat.Path }}</a>
					<div>
						<p> Aggregations:
							<table>
								<tr> 
									<td>Metric Name </td><td> Value </td><td> Total </td><td> Percent </td>
								</tr>
								<tr>
									<td>Count </td><td> {{ $stat.Count }} </td><td> {{ $stat.TotalCount }} </td><td> {{ $stat.PercentCount }}%  </td> 
								</tr>
								<tr>
									<td> Length </td><td> {{ $stat.Length }} </td><td> {{ $stat.TotalLength }} </td><td> {{ $stat.PercentLength}}% </td> 
								</tr>
								<tr>
									<td> Duration </td><td> {{ $stat.Duration }} </td><td> {{ $stat.TotalDuration }} </td><td> {{ $stat.PercentDuration}}% </td>
								</tr>
							</table>
						</p>
	
						<p> By Time:
							<table>
								<tr><td>Min</td><td>Max</td></tr>
								<tr><td>{{ $stat.RelativeTimeMin }}</td><td>{{ $stat.RelativeTimeMax }}</td></tr>
							</table>
						</p>
	
						<p> By PID: Total PIDs:{{ $pidCount := len $stat.PID }}{{$pidCount}}
							<table>
								<tr><td>PID</td><td>Relative Time</td></tr>
								{{ range $pid, $time := $stat.PID }}
									<tr><td>{{ $pid }}</td><td>{{ $time }}</td></tr>
								{{ end }}
							</table>
						</p>
	
						<p> By TID:
							<table>
								<tr><td>TID</td><td>Relative Time</td></tr>
								{{ range $tid, $time := .TID }}
									<tr><td>{{$tid}}</td><td>{{$time}}</td></tr>
								{{ end }}
							</table>
						</p>
	
						<p> By ParentPID:
							<table>
								<tr><td>ParentPID</td><td>Relative Time</td></tr>
								{{ range $pid, $time := $stat.ParentPID }}
									<tr><td>{{ $pid }}</td><td>{{ $time }}</td></tr>
								{{ end }}
							</table>
						</p>
	
						By operations:
						<table>
						{{ range $op, $opParams := $stat.Operation }}
							<tr>
								<td> {{$op}} </td> 
							{{ range $opParamName, $opParamVal := $opParams }}
								 <td> {{$opParamName}} </td><td> {{$opParamVal}} </td>
							{{end}}
							</tr>
						{{end}}
						</table>
					</div>
				{{end}}
			</div>
		{{else}}
			<div><strong>no rows</strong></div>
		{{end}}
		</div>
	</body>
</html>`

func procmonGenerateReportTopN(procmonStats map[string]map[string]map[string]procmonFileStats) error {
	for iteration, iterationStats := range procmonStats {
		browserFileStats := map[string][]procmonFileStats{}
		for browserProcessName, stat := range iterationStats {
			l := procmonGetListFromMap(stat)
			sort.Sort(procmonFileStatsSortByCount(l))
			browserFileStats[browserProcessName+" Count"] = lastN(l, 15)

			sort.Sort(procmonFileStatsSortByLength(l))
			browserFileStats[browserProcessName+" Length"] = lastN(l, 15)

			sort.Sort(procmonFileStatsSortByDuration(l))
			browserFileStats[browserProcessName+" Duration"] = lastN(l, 15)
			//fmt.Println("Len2: ", len(browserFileStats[browserProcessName]))
		}

		procmonReportJsonFileName := fmt.Sprintf("procmonTopN-%s.json", iteration)
		procmonReportJsonFile, err := os.OpenFile(filepath.Join(*csvPath, procmonReportJsonFileName), os.O_CREATE|os.O_TRUNC, 0666)
		if err != nil {
			if procmonReportJsonFile != nil {
				procmonReportJsonFile.Close()
			}
			return err
		}
		defer procmonReportJsonFile.Close()

		err = json.NewEncoder(procmonReportJsonFile).Encode(browserFileStats)
		if err != nil {
			fmt.Printf("failed to json encode %s: %s\n", procmonReportJsonFileName, err)
			return err
		}

		err = procmonGenerateHtmlReport("procmonTopN", procmonReportTplBaseTopN, iteration, browserFileStats)
		if err != nil {
			fmt.Printf("failed to procmonGenerateHtmlReport for iteration %s: %s\n", procmonReportJsonFileName, err)
			return err
		}
	}

	return nil
}

func procmonGenerateHtmlReport(name string, tpl string, iteration string, data interface{}) error {
	var fn = template.FuncMap{
		"noescape": func(v interface{}) template.HTML {
			return template.HTML(fmt.Sprint(v))
		},
	}

	t, err := template.New("procmon").Funcs(fn).Parse(tpl)
	if err != nil {
		return err
	}

	procmonReportHtmlFileName := fmt.Sprintf("%s-%s.html", name, iteration)
	procmonReportHtmlFile, err := os.OpenFile(filepath.Join(*csvPath, procmonReportHtmlFileName), os.O_CREATE|os.O_TRUNC, 0666)
	if err != nil {
		if procmonReportHtmlFile != nil {
			procmonReportHtmlFile.Close()
		}
		return err
	}
	defer procmonReportHtmlFile.Close()

	err = t.Execute(procmonReportHtmlFile, data)
	if err != nil {
		return err
	}
	return nil
}

func lastN(pfs []procmonFileStats, n int) []procmonFileStats {
	var reversed []procmonFileStats
	for i := len(pfs) - 1; i >= 0; i-- {
		reversed = append(reversed, pfs[i])
	}
	var lastN []procmonFileStats
	for i := 0; i <= n; i++ {
		lastN = append(lastN, reversed[i])
	}
	return lastN
}

// c:\path > procmonFileStats
func procmonGetListFromMap(pfs map[string]procmonFileStats) []procmonFileStats {
	var l []procmonFileStats

	for _, stat := range pfs {
		l = append(l, stat)
	}
	//fmt.Println("Len: ", len(l))
	return l
}

func procmonExport(inputPmlFile, exportFile string) error {
	cmd := exec.Command(
		"procmon.exe",
		"/SaveApplyFilter",
		"/LoadConfig", "ProcmonConfiguration.pmc",
		"/OpenLog", inputPmlFile,
		"/SaveAs", exportFile,
	)
	fmt.Printf("%s\n", strings.Join(cmd.Args, " "))
	err := cmd.Run()
	if err != nil {
		fmt.Printf("failed to procmonExport(%s, %s): %s\n", inputPmlFile, exportFile, err)
		return err
	}

	return nil
}
