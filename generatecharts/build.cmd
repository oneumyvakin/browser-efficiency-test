IF [%GOPATH%] == [] (
    echo "Skip build generatecharts.exe: GOPATH variable is not set. Exit."
    exit 0
)

set PKGNAME=generatecharts
set LOCALPATH=%~dp0

mklink /J "%GOPATH%\src\%PKGNAME%" "%LOCALPATH%"

go fmt %PKGNAME%

go build -o "..\bin\%PKGNAME%.exe" %PKGNAME%

rmdir "%GOPATH%\src\%PKGNAME%"