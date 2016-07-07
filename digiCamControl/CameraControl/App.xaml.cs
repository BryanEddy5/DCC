#region Licence

// Distributed under MIT License
// ===========================================================
// 
// digiCamControl - DSLR camera remote control open source software
// Copyright (C) 2014 Duka Istvan
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
// MERCHANTABILITY,FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY 
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
// THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#endregion

#region

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Database;
using CameraControl.Core.Interfaces;
using CameraControl.Core.Translation;
using CameraControl.Devices;
using CameraControl.Devices.Canon;
using CameraControl.Devices.Classes;
using CameraControl.windows;
using ImageMagick;
using MahApps.Metro;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using System.Drawing;
using System.Windows.Forms;

#endregion

namespace CameraControl
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private IMainWindowPlugin _basemainwindow;
        private StartUpWindow _startUpWindow;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Global exception handling  
            Current.DispatcherUnhandledException += AppDispatcherUnhandledException;

            string procName = Process.GetCurrentProcess().ProcessName;
            // get the list of all processes by that name

            Process[] processes = Process.GetProcessesByName(procName);

            if (processes.Length > 1)
            {
                MessageBox.Show(TranslationStrings.LabelApplicationAlreadyRunning);
                Shutdown(-1);
                return;
            }

            ThemeManager.AddAccent("Astro", new Uri("pack://application:,,,/CameraControl;component/Resources/AstroAccent.xaml"));
            ThemeManager.AddAppTheme("Black", new Uri("pack://application:,,,/CameraControl;component/Resources/AstroTheme.xaml"));

            ServiceProvider.Branding = Branding.LoadBranding();
            if (ServiceProvider.Branding.ShowStartupScreen)
            {
                _startUpWindow=new StartUpWindow();
                _startUpWindow.Show();
            }

            Task.Factory.StartNew(InitApplication);
        }

        private void InitApplication()
        {
            // prevent some application crash
            //WpfCommands.DisableWpfTabletSupport();

            Dispatcher.Invoke(new Action(ServiceProvider.Configure));
            

            ServiceProvider.Settings = new Settings();
            ServiceProvider.Settings = ServiceProvider.Settings.Load();
            ServiceProvider.Branding = Branding.LoadBranding();

            if (ServiceProvider.Settings.DisableNativeDrivers &&
                MessageBox.Show(TranslationStrings.MsgDisabledDrivers, "", MessageBoxButton.YesNo) ==
                MessageBoxResult.Yes)
                ServiceProvider.Settings.DisableNativeDrivers = false;
            ServiceProvider.Settings.LoadSessionData();
            TranslationManager.LoadLanguage(ServiceProvider.Settings.SelectedLanguage);
            
            ServiceProvider.PluginManager.CopyPlugins();
            Dispatcher.Invoke(new Action(InitWindowManager));


            ServiceProvider.Trigger.Start();
            ServiceProvider.Analytics.Start();



            Dispatcher.Invoke(new Action(delegate
            {
                try
                {
                    // event handlers
                    ServiceProvider.Settings.SessionSelected += Settings_SessionSelected;

                    ServiceProvider.DeviceManager.CameraConnected += DeviceManager_CameraConnected;
                    ServiceProvider.DeviceManager.CameraSelected += DeviceManager_CameraSelected;
                    ServiceProvider.DeviceManager.CameraDisconnected += DeviceManager_CameraDisconnected;
                    //-------------------
                    ServiceProvider.DeviceManager.DisableNativeDrivers = ServiceProvider.Settings.DisableNativeDrivers;
                    if (ServiceProvider.Settings.AddFakeCamera)
                        ServiceProvider.DeviceManager.AddFakeCamera();
                    ServiceProvider.DeviceManager.ConnectToCamera();
                }
                catch (Exception exception)
                {
                    Log.Error("Unable to initialize device manager", exception);
                    if (exception.Message.Contains("0AF10CEC-2ECD-4B92-9581-34F6AE0637F3"))
                    {
                        MessageBox.Show(
                            "Unable to initialize device manager !\nMissing some components! Please install latest Windows Media Player! ");
                        Application.Current.Shutdown(1);
                    }
                }
                StartApplication();
                if (_startUpWindow != null)
                    _startUpWindow.Close();

                ShowStartupWebPage(ServiceProvider.Settings);
                PrepImageLibrary();
            }));
            ServiceProvider.Database.Add(new DbEvents(EventType.AppStart));
        }

        private void ShowStartupWebPage(Settings settings)
        {
            // Start an initial local Web page - accept certificate and point to server page
            // See https://support.microsoft.com/en-us/kb/305703

            // string target = "https://localhost:5513/";
            string target = String.Format("https://localhost:{0}/", settings.WebserverPort);
            try
            {
                System.Diagnostics.Process.Start(target);
            }
            catch
                (
                 System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        private void InitWindowManager()
        {
            ServiceProvider.WindowsManager = new WindowsManager();
            ServiceProvider.PluginManager.LoadPlugins(Path.Combine(Settings.ApplicationFolder, "Plugins"));

            ServiceProvider.PluginManager.LoadPlugins(Path.Combine(Settings.ApplicationFolder, "Branding", "Plugins"));

            _basemainwindow = new MainWindow();
            ServiceProvider.PluginManager.MainWindowPlugins.Add(_basemainwindow);

            try
            {
                ServiceProvider.WindowsManager.Add(new FullScreenWnd());
                ServiceProvider.WindowsManager.Add(new LiveViewManager());
                ServiceProvider.WindowsManager.Add(new MultipleCameraWnd());
                ServiceProvider.WindowsManager.Add(new CameraPropertyWnd());
                ServiceProvider.WindowsManager.Add(new BrowseWnd());
                ServiceProvider.WindowsManager.Add(new TagSelectorWnd());
                ServiceProvider.WindowsManager.Add(new DownloadPhotosWnd());
                ServiceProvider.WindowsManager.Add(new BulbWnd());
                ServiceProvider.WindowsManager.Add(new AstroLiveViewWnd());
                ServiceProvider.WindowsManager.Add(new ScriptWnd());
                ServiceProvider.WindowsManager.Add(new PrintWnd());
                ServiceProvider.WindowsManager.Add(new TimeLapseWnd());
                ServiceProvider.WindowsManager.Add(new BarcodeWnd());
                //ServiceProvider.WindowsManager.Add(new StatisticsWnd());
                ServiceProvider.WindowsManager.Event += WindowsManager_Event;
                ServiceProvider.WindowsManager.ApplyTheme();
                //ServiceProvider.WindowsManager.ApplyKeyHanding();
                ServiceProvider.WindowsManager.RegisterKnowCommands();
                ServiceProvider.Settings.SyncActions(ServiceProvider.WindowsManager.WindowCommands);

                ServiceProvider.PluginManager.ToolPlugins.Add(new ScriptWnd());

                foreach (IPlugin plugin in ServiceProvider.PluginManager.Plugins)
                {
                    plugin.Init();
                }
            }
            catch (Exception exception)
            {
                Log.Error("Error to load plugins ", exception);
            }
        }

        private void DeviceManager_CameraDisconnected(ICameraDevice cameraDevice)
        {
            cameraDevice.CameraInitDone -= cameraDevice_CameraInitDone;
        }

        private void StartApplication()
        {
            if (!string.IsNullOrEmpty(ServiceProvider.Settings.SelectedMainForm) &&
                ServiceProvider.Settings.SelectedMainForm != _basemainwindow.DisplayName)
            {
                SelectorWnd wnd = new SelectorWnd();
                wnd.ShowDialog();
            }
            IMainWindowPlugin mainWindowPlugin = _basemainwindow;
            foreach (IMainWindowPlugin windowPlugin in ServiceProvider.PluginManager.MainWindowPlugins)
            {
                if (windowPlugin.DisplayName == ServiceProvider.Settings.SelectedMainForm)
                    mainWindowPlugin = windowPlugin;
            }
            ServiceProvider.PluginManager.SelectedWindow = mainWindowPlugin;
            mainWindowPlugin.Show();
            if (mainWindowPlugin is Window)
                ((Window)mainWindowPlugin).Activate();
        }

        private void PrepImageLibrary()
        {
            Dispatcher.Invoke(new Action(delegate
            {
                Log.Debug("Prepping Image API ...");
                var watch = System.Diagnostics.Stopwatch.StartNew();

                string imageSoftwareName = "ImageMagick";
                string localImageSoftwareDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), imageSoftwareName);
                string commonImageSoftwareDir = Path.Combine(Settings.DataFolder, imageSoftwareName);

                if (!Directory.Exists(localImageSoftwareDir))
                {
                    if (Directory.Exists(commonImageSoftwareDir))
                    {
                        Log.Debug("Prepping Image API, copying common files ...");
                        DirectoryCopy(commonImageSoftwareDir, localImageSoftwareDir, true);
                    }
                }
                watch.Stop();
                Log.Debug("ms for ImageMagick prep: " + watch.ElapsedMilliseconds);

                // Test that we're loaded and ready - this populates localImageMagickDir if needed
                watch = System.Diagnostics.Stopwatch.StartNew();
                MagickImage image = new MagickImage(new MagickColor(Color.Black), 100, 200);
                image.Thumbnail(50, 100);
                watch.Stop();
                Log.Debug("ms for ImageMagick test: " + watch.ElapsedMilliseconds);

                if (!Directory.Exists(localImageSoftwareDir)) {
                    Log.Error("Prepping Image API failed.  localImageMagickDir not created as expected: " + localImageSoftwareDir);
                }
                else if (!Directory.Exists(commonImageSoftwareDir))
                {
                    // This can happen with a copy from zip files rather than an installer
                    // Create the common directory for other users of the machine
                    DirectoryCopy(localImageSoftwareDir, commonImageSoftwareDir, true);
                }

                Log.Debug("Prepping Image API complete");
            }));
        }

        // From: https://msdn.microsoft.com/en-us/library/bb762914.aspx
        private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            System.IO.FileInfo[] files = dir.GetFiles();
            foreach (System.IO.FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private void WindowsManager_Event(string cmd, object o)
        {
            try
            {

                Log.Debug("Window command received :" + cmd);

                if (cmd != WindowsCmdConsts.Next_Image && cmd != WindowsCmdConsts.Prev_Image &&
                    cmd != WindowsCmdConsts.Select_Image && !cmd.StartsWith("Zoom"))
                    ServiceProvider.Analytics.Command(cmd, o as string);

                if (cmd == CmdConsts.All_Close)
                {
                    ServiceProvider.WindowsManager.Event -= WindowsManager_Event;
                    ServiceProvider.Analytics.Stop();
                    if (ServiceProvider.Settings != null)
                    {
                        ServiceProvider.Settings.Save(ServiceProvider.Settings.DefaultSession);
                        ServiceProvider.Settings.Save();
                        if (ServiceProvider.Trigger != null)
                        {
                            ServiceProvider.Trigger.Stop();
                        }
                    }
                    ServiceProvider.ScriptManager.Stop();
                    ServiceProvider.DeviceManager.CloseAll();
                    Thread.Sleep(1000);
                    Dispatcher.Invoke(new Action(() => Current.Shutdown()));
                }
                switch (cmd)
                {
                    case CmdConsts.Capture:
                        Thread thread = new Thread(new ThreadStart(CameraHelper.Capture));
                        thread.Start();
                        break;
                    case CmdConsts.CaptureNoAf:
                        CameraHelper.CaptureNoAf();
                        break;
                    case CmdConsts.CaptureAll:
                        CameraHelper.CaptureAll(0);
                        break;
                    case CmdConsts.NextSeries:
                        if (ServiceProvider.Settings != null) ServiceProvider.Settings.DefaultSession.Series++;
                        break;
                }
                ICameraDevice device = ServiceProvider.DeviceManager.SelectedCameraDevice;
                if (device != null && device.IsConnected)
                {
                    switch (cmd)
                    {
                        //case CmdConsts.ResetDevice:
                        //        device.ResetDevice();
                        //    break;
                        case CmdConsts.NextAperture:
                            if (device.FNumber != null)
                                device.FNumber.NextValue();
                            break;
                        case CmdConsts.PrevAperture:
                            if (device.FNumber != null)
                                device.FNumber.PrevValue();
                            break;
                        case CmdConsts.NextIso:
                            if (device.IsoNumber != null)
                                device.IsoNumber.NextValue();
                            break;
                        case CmdConsts.PrevIso:
                            if (device.IsoNumber != null)
                                device.IsoNumber.PrevValue();
                            break;
                        case CmdConsts.NextShutter:
                            if (device.ShutterSpeed != null)
                                device.ShutterSpeed.NextValue();
                            break;
                        case CmdConsts.PrevShutter:
                            if (device.ShutterSpeed != null)
                                device.ShutterSpeed.PrevValue();
                            break;
                        case CmdConsts.NextWhiteBalance:
                            if (device.WhiteBalance != null)
                                device.WhiteBalance.NextValue();
                            break;
                        case CmdConsts.PrevWhiteBalance:
                            if (device.WhiteBalance != null)
                                device.WhiteBalance.PrevValue();
                            break;
                        case CmdConsts.NextExposureCompensation:
                            if (device.ExposureCompensation != null)
                                device.ExposureCompensation.NextValue();
                            break;
                        case CmdConsts.PrevExposureCompensation:
                            if (device.ExposureCompensation != null)
                                device.ExposureCompensation.PrevValue();
                            break;
                        case CmdConsts.NextCamera:
                            ServiceProvider.DeviceManager.SelectNextCamera();
                            break;
                        case CmdConsts.PrevCamera:
                            ServiceProvider.DeviceManager.SelectPrevCamera();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error processing comand", ex);
            }
        }

        #region eventhandlers

        /// <summary>
        /// Called when default session is assigned or changed
        /// </summary>
        /// <param name="oldvalue">The oldvalue.</param>
        /// <param name="newvalue">The newvalue.</param>
        private void Settings_SessionSelected(PhotoSession oldvalue, PhotoSession newvalue)
        {
            // check if same session is used 
            if (oldvalue == newvalue)
                return;
            if (oldvalue != null && ServiceProvider.Settings.PhotoSessions.Contains(oldvalue))
                ServiceProvider.Settings.Save(oldvalue);
            ServiceProvider.QueueManager.Clear();
            if (ServiceProvider.DeviceManager.SelectedCameraDevice != null)
                ServiceProvider.DeviceManager.SelectedCameraDevice.AttachedPhotoSession = newvalue;
        }

        private void DeviceManager_CameraSelected(ICameraDevice oldcameraDevice, ICameraDevice newcameraDevice)
        {
            if (newcameraDevice == null)
                return;
            Log.Debug("DeviceManager_CameraSelected 1");
            var thread = new Thread(delegate()
            {
                CameraProperty property = newcameraDevice.LoadProperties();
                // load session data only if not session attached to the selected camera
                if (newcameraDevice.AttachedPhotoSession == null)
                {
                    newcameraDevice.AttachedPhotoSession =
                        ServiceProvider.Settings.GetSession(property.PhotoSessionName);
                }
                if (newcameraDevice.AttachedPhotoSession != null)
                    ServiceProvider.Settings.DefaultSession =
                        (PhotoSession)newcameraDevice.AttachedPhotoSession;
            });
            thread.Start();
            Log.Debug("DeviceManager_CameraSelected 2");
        }


        private void DeviceManager_CameraConnected(ICameraDevice cameraDevice)
        {
            cameraDevice.CameraInitDone += cameraDevice_CameraInitDone;
        }

        private void cameraDevice_CameraInitDone(ICameraDevice cameraDevice)
        {
            Log.Debug("cameraDevice_CameraInitDone 1");
            var property = cameraDevice.LoadProperties();
            CameraPreset preset = ServiceProvider.Settings.GetPreset(property.DefaultPresetName);
            // multiple canon cameras block with this settings
            Console.WriteLine(ServiceProvider.DeviceManager.ConnectedDevices.Count);

            if ((cameraDevice is CanonSDKBase && ServiceProvider.Settings.LoadCanonTransferMode) || !(cameraDevice is CanonSDKBase))
                cameraDevice.CaptureInSdRam = property.CaptureInSdRam;

            Log.Debug("cameraDevice_CameraInitDone 1a");
            if (ServiceProvider.Settings.SyncCameraDateTime)
            {
                try
                {
                    Log.Debug("set time 1");
                    cameraDevice.DateTime = DateTime.Now;
                    Log.Debug("set time 2");
                }
                catch (Exception exception)
                {
                    Log.Error("Unable to sysnc date time", exception);
                }
            }
            Log.Debug("cameraDevice_CameraInitDone 2");
            if (preset != null)
            {
                var thread = new Thread(delegate()
                {
                    try
                    {
                        Thread.Sleep(1500);
                        cameraDevice.WaitForCamera(5000);
                        preset.Set(cameraDevice);
                    }
                    catch (Exception e)
                    {
                        Log.Error("Unable to load default preset", e);
                    }
                });
                thread.Start();
            }
            Log.Debug("cameraDevice_CameraInitDone 3");
            ServiceProvider.Analytics.CameraConnected(cameraDevice);
        }

        #endregion


        private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            //#if DEBUG
            //      // In debug mode do not custom-handle the exception, let Visual Studio handle it

            //      e.Handled = false;

            //#else

            //          ShowUnhandeledException(e);    

            //#endif
            ShowUnhandeledException(e);
        }

        private void ShowUnhandeledException(DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            Log.Error("Unhandled error ", e.Exception);
            ServiceProvider.Analytics.Error(e.Exception);
            string errorMessage =
                string.Format(
                    TranslationStrings.LabelUnHandledError,
                    e.Exception.Message + (e.Exception.InnerException != null
                                               ? "\n" +
                                                 e.Exception.InnerException.Message
                                               : null));
            if (e.Exception.GetType() == typeof(MissingMethodException))
            {
                Log.Error("Damaged installation. Application exiting ");
                MessageBox.Show("Application crash !! Damaged installation!\nPlease unintall aplication from control panel and reinstall it!");
                if (Current != null)
                    Current.Shutdown();
            }
            // check if wia 2.0 is registered 
            // isn't a clean way
            if (errorMessage.Contains("{E1C5D730-7E97-4D8A-9E42-BBAE87C2059F}"))
            {
                MessageBox.Show(TranslationStrings.LabelWiaNotInstalled);
                PhotoUtils.RunAndWait("regwia.bat", "");
                MessageBox.Show(TranslationStrings.LabelRestartTheApplication);
                Application.Current.Shutdown();
            }
            else if (e.Exception.GetType() == typeof (OutOfMemoryException))
            {
                Log.Error("Out of memory. Application exiting ");
                MessageBox.Show(TranslationStrings.LabelOutOfMemory);
                if (Current != null)
                    Current.Shutdown();
            }
            else
            {
                if (MessageBox.Show(TranslationStrings.LabelAskSendLogFile, TranslationStrings.LabelApplicationError,
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    var wnd = new ErrorReportWnd("Application crash " + e.Exception.Message, e.Exception.StackTrace);
                    wnd.ShowDialog();
                }
                if (
                    MessageBox.Show(errorMessage, TranslationStrings.LabelApplicationError, MessageBoxButton.YesNo,
                                    MessageBoxImage.Error) ==
                    MessageBoxResult.No)
                {
                    if (Current != null)
                        Current.Shutdown();
                }
            }
        }
    }
}