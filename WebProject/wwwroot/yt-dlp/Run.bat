@echo OFF

 
cd "[The location where you have yt-dlp.exe]"
 
title Enter URL to Download
 
set /p URL=URL: 
 
cls
 
title Enter File Format
 
set /p format=Audio (a) or Video (v)?: 
 
cls
 
title Downloading...
 
if %format%==a yt-dlp.exe -P "[The location where you want downloaded files to appear]" -x -f "ba/b" -o "%%(title)s.%%(ext)s" -w %URL%
 
if %format%==v yt-dlp.exe -P "[The location where you want downloaded files to appear]" -f "bv+ba/b" -o "%%(title)s.%%(ext)s" -w %URL%
 
if %format%==a @echo Audio - %URL%>>"[The location where you have your log.txt file]" 
 
if %format%==v @echo Video - %URL%>>"[The location where you have your log.txt file]"
 
cls
 
title Download more?
 
set /p more=Do you want to download more? Yes (y) or No (n): 
 
if %more%==y start Run.bat
 
if %more%==y exit
 
if %more%==n goto b
 
:b
 
explorer "[The location where you want downloaded files to appear]"
 
exit