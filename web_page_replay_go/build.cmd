IF [%GOPATH%] == [] (
    echo "Skip build websrv.exe: GOPATH variable is not set. Exit."
    exit 0
)

rem Dependencies
rem go get "github.com/urfave/cli"
rem go get "golang.org/x/net/http2"

set LOCALPATH=%~dp0
set GLOBALGOPATH=%GOPATH%
set GOPATH=%GLOBALGOPATH%;%LOCALPATH%..\gopath
set GOOS=windows
set GOARCH=amd64


set PKGNAME=wpr
mklink /J "%GLOBALGOPATH%\src\%PKGNAME%" "%LOCALPATH%\%PKGNAME%"
go build -a -o "..\bin\WebPageReplay\WebPageReplay.exe" "%PKGNAME%"
set GOOS=linux
go build -a -o "..\bin\WebPageReplay\WebPageReplay.linux" "%PKGNAME%"
rmdir "%GLOBALGOPATH%\src\%PKGNAME%"

set GOOS=windows
set GOARCH=amd64
set PKGNAME=httparchive
mklink /J "%GLOBALGOPATH%\src\%PKGNAME%" "%LOCALPATH%\%PKGNAME%"
go build -a -o "..\bin\WebPageReplay\%PKGNAME%.exe" "%PKGNAME%"
rmdir "%GLOBALGOPATH%\src\%PKGNAME%"


xcopy /Y wpr_cert.pem "..\bin\WebPageReplay\wpr_cert.pem"
xcopy /Y wpr_key.pem  "..\bin\WebPageReplay\wpr_key.pem"
xcopy /Y ca.crt       "..\bin\WebPageReplay\ca.crt"
xcopy /Y ca.key       "..\bin\WebPageReplay\ca.key"
xcopy /Y genTls.cmd   "..\bin\WebPageReplay\genTls.cmd"

set GOPATH=%GLOBALGOPATH%