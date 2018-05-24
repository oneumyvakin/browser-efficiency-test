IF [%GOPATH%] == [] (
    echo "Skip build: GOPATH variable is not set. Exit."
    exit 0
)

set PKGNAME=remoteagent
set LOCALPATH=%~dp0

mklink /J "%GOPATH%\src\%PKGNAME%" "%LOCALPATH%"

set GOOS=windows
set GOARCH=amd64
go build -a -o "..\bin\%PKGNAME%.exe" %PKGNAME%

set GOOS=linux
set GOARCH=amd64
go build -o "..\bin\%PKGNAME%.linux" %PKGNAME%

set GOOS=darwin
set GOARCH=amd64
go build -o "..\bin\%PKGNAME%.mac" %PKGNAME%

rmdir "%GOPATH%\src\%PKGNAME%"