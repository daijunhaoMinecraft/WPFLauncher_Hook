@echo off
nuitka --standalone --onefile --disable-console --include-package=curl_cffi --include-package=wx.html2 --output-filename=LoginBy4399.exe --jobs=16 main.py
pause