set logfile=c:\temp\certificate\tmp.log
date /T > %logfile%
time /T >> %logfile%
echo pwd is %CD% >> %logfile%
whoami >> %logfile%
echo args are %* >> %logfile%

REM C:\WINDOWS\system32\certutil.exe -p webcat -importpfx C:\temp\certificate\AmazonWebCatBundle.pfx >> %logfile%
REM C:\temp\certificate\CertificateInstaller.exe -p webcat -importpfx C:\temp\certificate\AmazonWebCatBundle.pfx >> %logfile%
Tools\CertificateInstaller.exe %* >> %logfile%
