@echo off
REM Launch the Witch card design page + open it in the browser.
start "" http://localhost:7820
node "%~dp0card-data\server.js"
