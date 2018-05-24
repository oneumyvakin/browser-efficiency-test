package main

import (
	"github.com/cloverstd/parse-string-argv"
	"github.com/oneumyvakin/osext"

	"net/http"
	"log"
	"fmt"
	"flag"
	"os"
	"crypto/tls"
	"os/exec"
	"encoding/json"
	"path/filepath"
	"io/ioutil"
	"runtime"
	"strings"
)

const (
	configFileName       = "remoteagent.json"
	configDefaultListen  = ""
	configDefaultPort    = "8086"
	configDefaultPortTLS = "8087"
)

var (
	logger        *log.Logger
)

type WebSrv struct {
	webRoot string
}

type RemoteAgentConfig struct {
	Listen string `json:"listen"`
	Port string `json:"port"`
	PortTls string `json:"portTls"`
}

type RemoteAgentCommand struct {
	Cmd string `json:"cmd"`
	Args string `json:"args"`
}

func main() {
	root, err := osext.ExecutableFolder()
	if err != nil {
		log.Fatalf("Failed to get current dir: %s %s", root, err)
	}
	logger = newLogger(filepath.Join(root, "remoteagent.log"))

	raConfig, err := NewRemoteAgentConfig()
	if err != nil {
		logger.Printf("failed apply config %s: '%s'\n", configFileName, err.Error())
	}

	host := flag.String("host", raConfig.Listen, "Listen address. Default: all")
	port := flag.String("port", raConfig.Port, "Listen port for not-secure connections. Default: 8086")
	portTls := flag.String("portTls", raConfig.PortTls, "Listen port for secure connections. Default: 8087")
	tlsCertFile := flag.String("tlsCertFile", "websrv-tls.crt", "TLS certificate file path. Default: websrv-tls.crt")
	tlsKeyFile := flag.String("tlsKeyFile", "websrv-tls.key", "TLS key file path. Default: websrv-tls.key")
	webRoot := flag.String("webRoot", root, "Absolute path to web root. Default: current directory")
	flag.Parse()

	webSrv := WebSrv{webRoot:*webRoot}

	http.HandleFunc("/", webSrv.index)

	_, errCrtFile := os.Stat(*tlsCertFile)
	_, errKeyFile := os.Stat(*tlsKeyFile)
	if errCrtFile == nil && errKeyFile == nil {
		tlsServer := &http.Server{
			Addr: fmt.Sprintf("%s:%s", *host, *portTls),
			Handler: nil,
			TLSNextProto: map[string]func(*http.Server, *tls.Conn, http.Handler){},
			TLSConfig: &tls.Config{
				MinVersion:               tls.VersionTLS10,
				PreferServerCipherSuites: true,
			},
		}
		go func() {
			err = tlsServer.ListenAndServeTLS(*tlsCertFile, *tlsKeyFile)
			fmt.Printf("tlsServer error: '%s'\n", err)
		}()
	} else {
		fmt.Printf("errCrtFile: '%s'\n", errCrtFile)
		fmt.Printf("errKeyFile: '%s'\n", errKeyFile)
	}

	panic(http.ListenAndServe(fmt.Sprintf("%s:%s", *host, *port), nil))
}

func NewRemoteAgentConfig() (RemoteAgentConfig, error) {
	rac := RemoteAgentConfig{
		Listen:  configDefaultListen,
		Port:    configDefaultPort,
		PortTls: configDefaultPortTLS,
	}

	if _, err := os.Stat(configFileName); err != nil {
		return rac, err
	}

	configFile, err := ioutil.ReadFile(configFileName)
	if err != nil {
		return rac, err
	}

	err = json.Unmarshal(configFile, &rac)
	if err != nil {
		return rac, err
	}

	return rac, nil
}

func (webSrv WebSrv) index(rw http.ResponseWriter, req *http.Request) {
	if req.Method == "GET" {
		errMsg := "method GET not allowed\r\n"
		logger.Print(errMsg)
		http.Error(rw, errMsg, 403)
		return
	}
	rw.Header().Set("Access-Control-Allow-Origin", "*")
	rw.Header().Set("Cache-Control", "no-cache, no-store, must-revalidate")

	defer req.Body.Close()

	decoder := json.NewDecoder(req.Body)

	remoteCmd := RemoteAgentCommand{}
	err := decoder.Decode(&remoteCmd)
	if err != nil {
		errMsg := fmt.Sprintf("failed to decode json: '%s'\r\n", err.Error())
		logger.Print(errMsg)
		http.Error(rw, errMsg, 400)
		return
	}

	if runtime.GOOS == "linux" {
		remoteCmd.Cmd = strings.Replace(remoteCmd.Cmd, `\`, "/", -1)
		remoteCmd.Args = strings.Replace(remoteCmd.Args, `\`, "/", -1)
	}

	args, err := stringargv.Parse(remoteCmd.Args)
	if err != nil {
		errMsg := fmt.Sprintf("failed to parse args '%s' '%s': %s\r\n", remoteCmd.Cmd, remoteCmd.Args, err)
		logger.Print(errMsg)
		http.Error(rw, errMsg, 400)
		return
	}

	cmd := exec.Command(
		remoteCmd.Cmd,
		args...,
	)

	go cmdRunner(cmd)

	msg := fmt.Sprintf("execute on RemoteAgent: \"%s\" %s\r\n", remoteCmd.Cmd, args)
	fmt.Fprintf(rw, msg)
	logger.Print(msg)
}

func cmdRunner(cmd *exec.Cmd) {
	err := cmd.Run()
	if err != nil {
		logger.Printf("failed to execute '%s' '%s': %s\r\n", cmd.Path, cmd.Args, err)
		return
	}
}

func newLogger(logFilePath string) *log.Logger {
	logFile, err := os.OpenFile(logFilePath, os.O_CREATE|os.O_WRONLY|os.O_APPEND, 0660)
	if err != nil {
		log.Fatal("Failed to open log file "+logFilePath, err)
	}

	return log.New(logFile, os.Args[0]+": ", log.Ldate|log.Ltime|log.Lshortfile)
}