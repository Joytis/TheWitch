@echo off
REM Launch the card design page (Witch + Augur tabs) + open it in the browser.
start "" http://localhost:7820
node "%~dp0card-data\server.js"
