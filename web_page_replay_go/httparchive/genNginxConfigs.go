package main

import (
	"text/template"
	"fmt"
	"os"
	"path/filepath"
)

const (
	tplNginxServer = `
server {
		listen 80;
        listen 443 ssl;
        server_name  {{ .DomainName }};

        ssl_certificate {{ .CertDir }}/{{ .DomainName }}.crt;
        ssl_certificate_key {{ .CertDir }}/{{ .DomainName }}.key;

        root /var/www/html;

        index index.html;

        location / {
                proxy_set_header Host $host;
                proxy_pass http://{{ .WprIpAddress }}:{{ .WprPort }}$request_uri;
        }
}

`
)

func genNginxServer(certDir string, domainName string, wprIpAddress string, wprPort string, outDir string) error {
	serverConf := fmt.Sprintf("%s.conf", domainName)
	serverConfFile, err := os.OpenFile(filepath.Join(outDir, serverConf), os.O_CREATE|os.O_TRUNC, 0666)
	if err != nil {
		if serverConfFile != nil {
			serverConfFile.Close()
		}
		return err
	}
	defer serverConfFile.Close()

	t, err := template.New("server").Parse(tplNginxServer)
	if err != nil {
		return err
	}

	data := struct {
		CertDir string
		DomainName string
		WprIpAddress string
		WprPort string
	}{
		CertDir: certDir,
		DomainName: domainName,
		WprIpAddress: wprIpAddress,
		WprPort: wprPort,
	}
	err = t.Execute(serverConfFile, data)
	if err != nil {
		return err
	}
	return nil
}