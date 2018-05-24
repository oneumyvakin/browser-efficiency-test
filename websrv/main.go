package main

import (
	"github.com/oneumyvakin/osext"
	"net/http"
	"log"
	"fmt"
	"time"
	"flag"
	"os"
	"strconv"
	"net/url"
	"net/http/httputil"
	"crypto/tls"
)



var (
	reverseProxy *httputil.ReverseProxy

	cipherMap = map[string]uint16{
		"TLS_RSA_WITH_RC4_128_SHA": tls.TLS_RSA_WITH_RC4_128_SHA,
		"TLS_RSA_WITH_3DES_EDE_CBC_SHA": tls.TLS_RSA_WITH_3DES_EDE_CBC_SHA,
		"TLS_RSA_WITH_AES_128_CBC_SHA": tls.TLS_RSA_WITH_AES_128_CBC_SHA,
		"TLS_RSA_WITH_AES_256_CBC_SHA": tls.TLS_RSA_WITH_AES_256_CBC_SHA,
		"TLS_RSA_WITH_AES_128_CBC_SHA256": tls.TLS_RSA_WITH_AES_128_CBC_SHA256,
		"TLS_RSA_WITH_AES_128_GCM_SHA256": tls.TLS_RSA_WITH_AES_128_GCM_SHA256,
		"TLS_RSA_WITH_AES_256_GCM_SHA384": tls.TLS_RSA_WITH_AES_256_GCM_SHA384,
		"TLS_ECDHE_ECDSA_WITH_RC4_128_SHA": tls.TLS_ECDHE_ECDSA_WITH_RC4_128_SHA,
		"TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA": tls.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA,
		"TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA": tls.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA,
		"TLS_ECDHE_RSA_WITH_RC4_128_SHA": tls.TLS_ECDHE_RSA_WITH_RC4_128_SHA,
		"TLS_ECDHE_RSA_WITH_3DES_EDE_CBC_SHA": tls.TLS_ECDHE_RSA_WITH_3DES_EDE_CBC_SHA,
		"TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA": tls.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA,
		"TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA": tls.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA,
		"TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256": tls.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256,
		"TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256": tls.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256,
		"TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256": tls.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
		"TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256": tls.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
		"TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384": tls.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
		"TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384": tls.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
		"TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305": tls.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305,
		"TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305": tls.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305,
	}
)

type ReProxy struct {
	RawURL string `json:"url"`
	url *url.URL
}

type WebSrv struct {
	webRoot string
}

func main() {
	root, err := osext.ExecutableFolder()
	if err != nil {
		log.Fatalf("Failed to get current dir: %s %s", root, err)
	}
	port := flag.String("port", "8080", "Listen port for not-secure connections. Default: 8080")
	portTls := flag.String("portTls", "8081", "Listen port for secure connections. Default: 8081")
	tlsCertFile := flag.String("tlsCertFile", "websrv-tls.crt", "TLS certificate file path. Default: websrv-tls.crt")
	tlsKeyFile := flag.String("tlsKeyFile", "websrv-tls.key", "TLS key file path. Default: websrv-tls.key")
	tlsCipher := flag.String("tlsCipher", "TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256", "Cipher in RFC format. Default: TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256")
	stopAfter := flag.Int("stopAfter", 0, "Stop web server after n seconds. Default: run forever")
	webRoot := flag.String("webRoot", root, "Absolute path to web root. Default: current directory")
	proxyTo := flag.String("proxyTo", "", "Switch on proxy mode to provided URL like https://ya.ru. Default: disabled")
	flag.Parse()

	if *stopAfter > 0 {
		after := time.Second * time.Duration(*stopAfter)
		time.AfterFunc(after, func(){
			log.Println("Timeout!")
			os.Exit(0)
			})
	}

	if *proxyTo != "" {
		re := ReProxy{RawURL: *proxyTo}
		var err error
		re.url, err = url.Parse(re.RawURL)
		if err != nil {
			log.Fatal(err)
		}
		reverseProxy = httputil.NewSingleHostReverseProxy(re.url)
		reverseProxy.Director = re.director
		http.HandleFunc("/", re.index)

		panic(http.ListenAndServe(fmt.Sprintf(":%s", *port), nil))
	}

	webSrv := WebSrv{webRoot:*webRoot}

	http.HandleFunc("/", webSrv.index)
	http.HandleFunc("/slow", webSrv.slow)
	http.HandleFunc("/slowopen", webSrv.slowOpen)
	http.HandleFunc("/stop", stop)

	_, errCrtFile := os.Stat(*tlsCertFile)
	_, errKeyFile := os.Stat(*tlsKeyFile)
	if errCrtFile == nil && errKeyFile == nil {
		tlsServer := &http.Server{
			Addr: fmt.Sprintf(":%s", *portTls),
			Handler: nil,
			TLSNextProto: map[string]func(*http.Server, *tls.Conn, http.Handler){},
			TLSConfig: &tls.Config{
				CipherSuites: []uint16{
					cipherMap[*tlsCipher],
				},
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

	panic(http.ListenAndServe(fmt.Sprintf(":%s", *port), nil))
}

func (webSrv WebSrv) index(rw http.ResponseWriter, req *http.Request) {
	rw.Header().Set("Access-Control-Allow-Origin", "*")
	rw.Header().Set("Cache-Control", "no-cache, no-store, must-revalidate")
	fs := http.FileServer(http.Dir(webSrv.webRoot))
	fs.ServeHTTP(rw, req)
}

func (webSrv WebSrv) stop(rw http.ResponseWriter, req *http.Request) {
	rw.Header().Set("Access-Control-Allow-Origin", "*")
	rw.Header().Set("Cache-Control", "no-cache, no-store, must-revalidate")
	fs := http.FileServer(http.Dir(webSrv.webRoot))
	fs.ServeHTTP(rw, req)
}

func (re ReProxy) index(rw http.ResponseWriter, req *http.Request) {
	reverseProxy.ServeHTTP(rw, req)
}

func (re ReProxy) director(req *http.Request) {
	req.Host = re.url.Host
	req.URL.Scheme = re.url.Scheme
	req.URL.Host = re.url.Host
	/*
	// Dumping to stderr cause hangs of BrowserEfficienceTest.exe
	rawReq, err := httputil.DumpRequest(req, true)
	if err != nil {
		log.Printf("Fail to DumpRequest %s: %s", req.RequestURI, err)
		return
	}

	log.Printf("Request: %s", string(rawReq))
	*/
}

func stop(w http.ResponseWriter, r *http.Request) {
	keys, ok := r.URL.Query()["from"]
	from := "Unknown"
	if ok || len(keys) > 0 {
		from = keys[0]
	}

	fmt.Fprintf(w, "Stop from %s!", from)
	fmt.Printf( "Stop from %s!", from)
	os.Exit(0)
}

func (webSrv WebSrv) slow(rw http.ResponseWriter, req *http.Request) {
	keys, ok := req.URL.Query()["s"]

	waiting := time.Second * 10
	if ok || len(keys) > 0 {
		s, err := strconv.Atoi(keys[0])
		if err == nil {
			waiting = time.Second * time.Duration(s)
		}
	}
	time.Sleep(waiting)

	rw.Header().Set("Access-Control-Allow-Origin", "*")
	rw.Header().Set("Cache-Control", "no-cache, no-store, must-revalidate")
	fs := http.FileServer(http.Dir(webSrv.webRoot))
	fs.ServeHTTP(rw, req)
}

func (webSrv WebSrv) slowOpen(rw http.ResponseWriter, req *http.Request) {
	keysRedirect, foundRedirect := req.URL.Query()["redirect"]
	redirect := ""
	if foundRedirect || len(keysRedirect) > 0 {
		var err error
		redirect, err = url.PathUnescape(keysRedirect[0])
		if err != nil {
			fmt.Printf("error at PathUnescape '%s': %s\n", keysRedirect[0], err)
		}
	}

	keysSeconds, foundSeconds := req.URL.Query()["s"]
	waiting := time.Second * 10 // Default
	if foundSeconds || len(keysSeconds) > 0 {
		s, err := strconv.Atoi(keysSeconds[0])
		if err == nil {
			waiting = time.Second * time.Duration(s)
		}
	}
	time.Sleep(waiting)

	rw.Header().Set("Access-Control-Allow-Origin", "*")
	rw.Header().Set("Cache-Control", "no-cache, no-store, must-revalidate")
	if redirect != "" {
		rw.Header().Set("Location", redirect)
		rw.WriteHeader(http.StatusSeeOther)
		return
	}

	fs := http.FileServer(http.Dir(webSrv.webRoot))
	fs.ServeHTTP(rw, req)
}