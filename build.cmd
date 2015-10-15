@echo off
setlocal

if not defined NERVENMSBUILDCMD (for /D %%D in ("%ProgramFiles(x86)%\MSBuild\14.0\Bin\") do set NERVENMSBUILDCMD=%%~dpD\MSBuild.exe)
if not defined NERVENMSBUILDCMD (set NERVENMSBUILDCMD=MSBuild.exe)
echo Using MSBuild executable: %NERVENMSBUILDCMD%

if not defined NERVENBUILDFILE (for %%F in (*.sln) do set NERVENBUILDFILE=%%~nF.proj)
if not defined NERVENBUILDFILE (set NERVENBUILDFILE=build.proj)
if not exist %~dp0%NERVENBUILDFILE% (set NERVENBUILDFILE=build.proj)
echo Using MSBuild file: %NERVENBUILDFILE%

if not defined NERVENSOLUTIONFILE (for %%F in (*.sln) do set NERVENSOLUTIONFILE=%%F)
echo Using solution file: %NERVENSOLUTIONFILE%

if not defined NERVENSOLUTIONNAME (for %%F in (*.sln) do set NERVENSOLUTIONNAME=%%~nF)
echo Using solution name: %NERVENSOLUTIONNAME%

if not defined NERVENMSBUILDTARGET (set NERVENMSBUILDTARGET=%~1)
echo Using target: %NERVENMSBUILDTARGET%

if not exist "packages\" ("%NERVENMSBUILDCMD%" "%NERVENBUILDFILE%" /target:RestoreNuget)
if defined NERVENMSBUILDTARGET (set _targetArgument=/target:"%NERVENMSBUILDTARGET%")
"%NERVENMSBUILDCMD%" "%NERVENBUILDFILE%" %_targetArgument%
