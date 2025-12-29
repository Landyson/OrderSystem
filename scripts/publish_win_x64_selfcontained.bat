@echo off
setlocal
cd /d %~dp0\..
cd src\OrderSystem.Web
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
echo Done.
echo Output: src\OrderSystem.Web\bin\Release\net8.0\win-x64\publish\
