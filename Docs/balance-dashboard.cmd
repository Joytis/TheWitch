@echo off
REM Rebuild the Witch balance dashboard from Docs/card-data/*.json and open it locally.
node "%~dp0card-data\balance.js"
start "" "%~dp0balance-dashboard.html"
