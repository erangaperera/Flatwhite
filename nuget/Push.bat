@echo OFF
@echo Publishing following 2 packs:
@echo:
DIR /B *.nupkg
@echo:
SETLOCAL
SET VERSION=1.0.1
pause
nuget push Flatwhite.%VERSION%.nupkg
nuget push Flatwhite.Autofac.%VERSION%.nupkg
pause
ENDLOCAL