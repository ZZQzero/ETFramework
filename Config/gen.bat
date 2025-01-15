set WORKSPACE=.

set LUBAN_DLL=%WORKSPACE%\Luban\Luban.dll
set CONF_ROOT=%WORKSPACE%

dotnet %LUBAN_DLL% ^
    -t all ^
    -c cs-bin ^
    -d bin  ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=..\Unity\Assets/Gen ^
    -x outputDataDir=..\Unity\Assets\GenerateDatas\bytes
pause