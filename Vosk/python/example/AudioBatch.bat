@echo off

cd %cd%

(for /f "delims=" %%i in ('dir /b /s /a-d *.wav ') do (
    echo;%%~nxi
python3 test_simple.py %%~nxi
))


pause

