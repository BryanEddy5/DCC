REM %1 = Solution Directory
REM %2 = $(ConfigurationName) Debug/Release

copy %1DccObsPlugin\Bin\%2\DccObsPlugin.dll %1CameraControl\Bin\%2\ObsPlugin\CLRHostPlugin\ /Y 
