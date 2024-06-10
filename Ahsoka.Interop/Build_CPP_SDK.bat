echo off

SET VS_PATH=C:\Program Files\Microsoft Visual Studio\2022\Enterprise
if not exist "%VS_PATH%\" SET VS_PATH=C:\Program Files\Microsoft Visual Studio\2022\Professional
if not exist "%VS_PATH%\" SET VS_PATH=C:\Program Files\Microsoft Visual Studio\2022\Community
echo Using VS_PATH %VS_PATH%

call "%VS_PATH%\VC\Auxiliary\Build\vcvars64.bat"
if %ERRORLEVEL% NEQ 0 exit /b 1
cd %~dp0
cd ..
echo Building From Root Path %cd%

SET BUILD_TYPE="Debug"
SET AHSOKA_ROOT=%cd%
SET AHSOKA_BUILD_ROOT=%AHSOKA_ROOT%\Build
SET AHSOKA_BUILD_OUTPUT_ROOT=%AHSOKA_ROOT%\BuildOutputs\SDK

SET AHSOKA_BUILD=%AHSOKA_BUILD_ROOT%\Windows64
SET AHSOKA_BUILD_OUTPUT=%AHSOKA_BUILD_OUTPUT_ROOT%\Windows64_TargetSupport\SDK

echo AHSOKA_ROOT %AHSOKA_ROOT%
echo AHSOKA_BUILD %AHSOKA_BUILD%
echo AHSOKA_BUILD_OUTPUT %AHSOKA_BUILD_OUTPUT%

REM Clean Build Outputs
rmdir /s /q %AHSOKA_BUILD%
rmdir /s /q %AHSOKA_BUILD_OUTPUT%

REM Build Output Directory
if not exist %AHSOKA_BUILD_ROOT%\nul mkdir %AHSOKA_BUILD_ROOT%
mkdir %AHSOKA_BUILD%

REM Configure and Build for WINDOWS
cmake -DCMAKE_BUILD_TYPE=Debug ^
        -DCMAKE_C_COMPILER=cl.exe ^
        -DCMAKE_CXX_COMPILER=cl.exe ^
        -DAHSOKA_BUILD_OUTPUTS=%AHSOKA_BUILD_OUTPUT% ^
        -DCMAKE_INSTALL_PREFIX=%AHSOKA_BUILD_OUTPUT% ^
        -S%AHSOKA_ROOT% ^
        -B%AHSOKA_BUILD% ^
        -G Ninja 
if %ERRORLEVEL% NEQ 0 exit /b 1

cmake --build %AHSOKA_BUILD%
