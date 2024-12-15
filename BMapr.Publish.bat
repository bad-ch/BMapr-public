@echo off
REM Script to backup the share to a date/time stamped archive
REM By R.R
cls
set year=%date:~6,4%
set yr=%date:~8,2%
set month=%date:~3,2%
set day=%date:~0,2%
set hour=%time:~0,2%
set hour=%hour: =0%
set min=%time:~3,2%
set sec=%time:~6,2%

echo Backing up BMapr.GDAL branch ...


iisreset

"C:\Program Files\WinRAR\winrar.exe" a -r ../BMapr.Publish_%year%%month%%day%_%hour%%min%%sec%.rar C:\Users\webadmin\source\repos\Hexagon.GDAL\BMapr.Publish\*.*
