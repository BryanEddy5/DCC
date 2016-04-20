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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CameraControl.Core.Scripting;
using CameraControl.Core.Response;
using CameraControl.Devices;
using Griffin.WebServer;
using Griffin.WebServer.Files;
using Griffin.WebServer.Modules;
using Newtonsoft.Json;
using Griffin.Net.Protocols.Http;
using System.Runtime.CompilerServices;

#endregion

namespace CameraControl.Core.Classes
{
    public class WebServerModule : IWorkerModule
    {
        /*        private string _lineFormat =
                    "{image : '@image@', title : 'Image Credit: Maria Kazvan', thumb : '@image_thumb@', url : '@image_url@'},";*/

        //private string _lineFormat =
        //    "            <div>  <img u=\"image\" src=\"@image@\" /><img u=\"thumb\" src=\"@image_thumb@\" /></div>";

        private string _lineFormat =
    "            <img u=\"image\" src=\"@image@\" />";

        private bool _liveViewFirstRun = true;

        #region Implementation of IHttpModule

        public void BeginRequest(IHttpContext context)
        {
        }

        public void EndRequest(IHttpContext context)
        {
        }

        public void HandleRequestAsync(IHttpContext context, Action<IAsyncModuleResult> callback)
        {
            callback(new AsyncModuleResult(context, HandleRequest(context)));
        }

        #endregion

        public ModuleResult HandleRequest(IHttpContext context)
        {
            try
            {
                if (string.IsNullOrEmpty(context.Request.Uri.AbsolutePath) || context.Request.Uri.AbsolutePath == "/")
                {
                    string str = context.Request.Uri.Scheme + "://" + context.Request.Uri.Host;
                    if (context.Request.Uri.Port != 80)
                        str = str + (object)":" + context.Request.Uri.Port;
                    string uriString = str + context.Request.Uri.AbsolutePath + "index.html";
                    if (!string.IsNullOrEmpty(context.Request.Uri.Query))
                    {
                        // this is for backward compatibility with old Griffin.WebServer
                        string questionMark = !context.Request.Uri.Query.StartsWith("?") ? "?" : "";
                        uriString = uriString + questionMark + context.Request.Uri.Query;
                    }
                    context.Request.Uri = new Uri(uriString);
                }

                var queryString = ((HttpRequest)context.Request).QueryString;

                if (context.Request.Uri.AbsolutePath.StartsWith("/thumb/large"))
                {
                    string requestFile = Path.GetFileName(context.Request.Uri.AbsolutePath.Replace("/", "\\"));
                    foreach (FileItem item in ServiceProvider.Settings.DefaultSession.Files)
                    {
                        if (Path.GetFileName(item.LargeThumb) ==requestFile || item.Name == requestFile)
                        {
                            SendFile(context,
                                !File.Exists(item.LargeThumb)
                                    ? Path.Combine(Settings.WebServerFolder, "logo.png")
                                    : item.LargeThumb);
                            SendFile(context, item.LargeThumb);
                            return ModuleResult.Continue;
                        }
                    }
                }

                if (context.Request.Uri.AbsolutePath.StartsWith("/thumb/small"))
                {
                    string requestFile = Path.GetFileName(context.Request.Uri.AbsolutePath.Replace("/", "\\"));
                    foreach (FileItem item in ServiceProvider.Settings.DefaultSession.Files)
                    {
                        if (Path.GetFileName(item.SmallThumb) == requestFile || item.Name == requestFile)
                        {
                            SendFile(context,
                                !File.Exists(item.SmallThumb)
                                    ? Path.Combine(Settings.WebServerFolder, "logo.png")
                                    : item.SmallThumb);
                            return ModuleResult.Continue;
                        }
                    }
                }

                if (context.Request.Uri.AbsolutePath.StartsWith("/thumb/quick"))
                {
                    string requestFile = Path.GetFileName(context.Request.Uri.AbsolutePath.Replace("/", "\\"));
                    foreach (FileItem item in ServiceProvider.Settings.DefaultSession.Files)
                    {
                        if (Path.GetFileName(item.SmallThumb) == requestFile || item.Name == requestFile)
                        {
                            PhotoUtils.WaitForFile(item.QuickThumb);
                            SendFile(context,
                                !File.Exists(item.QuickThumb)
                                    ? Path.Combine(Settings.WebServerFolder, "logo.png")
                                    : item.QuickThumb);
                            return ModuleResult.Continue;
                        }
                    }
                }

                if (context.Request.Uri.AbsolutePath.StartsWith("/preview.jpg"))
                {
                    SendFile(context, ServiceProvider.Settings.SelectedBitmap.FileItem.LargeThumb);
                }

                if (context.Request.Uri.AbsolutePath.StartsWith("/session.json"))
                {
                    var s = JsonConvert.SerializeObject(ServiceProvider.Settings.DefaultSession, Formatting.Indented);
                    SendData(context, Encoding.ASCII.GetBytes(s));
                }

                if (context.Request.Uri.AbsolutePath.StartsWith("/thumb/captured"))
                {
                    string quickThumb = ServiceProvider.DeviceManager.JustCapturedImagePreview[ServiceProvider.DeviceManager.SelectedCameraDevice];
                            SendFile(context,
                                !File.Exists(quickThumb)
                                    ? Path.Combine(Settings.WebServerFolder, "logo.png")
                                    : quickThumb);
                            return ModuleResult.Continue;
                }

                if (context.Request.Uri.AbsolutePath.StartsWith("/jsonp.api"))
                {
                    // var operation = context.Request.QueryString["operation"];
                    var operation = queryString["operation"];

                    // someCallBackString({ The Object });
                    var jsoncallback = queryString["jsoncallback"];

                    Log.Debug("jsonp.api operation is " + operation + " - started");
                    if ("capture".Equals(operation))
                    {
                        string response = "undefined";
                        ICameraDevice selectedCameraDevice = ServiceProvider.DeviceManager.SelectedCameraDevice;
                        if (selectedCameraDevice == null || !selectedCameraDevice.IsConnected)
                        {
                            response = "Camera Disconnected";
                        }
                        else {
                            // Mark any previous images for deletion
                            foreach (FileItem item in ServiceProvider.Settings.DefaultSession.Files)
                            {
                                item.IsChecked = true;
                            }

                            string camera = queryString["camera"];
                            string param1 = queryString["param1"] ?? "";
                            string param2 = queryString["param2"] ?? "";
                            string[] args = new[] { operation, param1, param2 };
                            response = TakePicture(camera, args);
                        }
                        Log.Debug("TakePicture respose is: " + response);
                        if (response != "OK")
                        {
                            Log.Debug("Sending error response: " + response);
                            // context.Response.StatusCode = 417;

                            CaptureResponse captureResponse = new CaptureResponse(response);
                            var s = ResponseUtils.jsonpResponse(jsoncallback, JsonConvert.SerializeObject(captureResponse));

                            SendData(context, Encoding.ASCII.GetBytes(s));

                            Log.Debug("Reseting LiveView due to error: " + response);
                            StopLiveViewIfNeeded();
                            StartLiveViewIfNeeded();
                        }
                        else
                        {
                            CameraHelper.WaitPhotoProcessed();

                            // Remove any previous image
                            ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.Del_Image, true);
                            Log.Debug("Called ExecuteCommand with WindowsCmdConsts.Del_Image");

                            var fileName = ServiceProvider.DeviceManager.JustCapturedImage[ServiceProvider.DeviceManager.SelectedCameraDevice];
                            string id = ServiceProvider.DeviceManager.JustCapturedImageId[ServiceProvider.DeviceManager.SelectedCameraDevice];

                            CaptureResponse captureResponse = new CaptureResponse(response, fileName, id);
                            var s = ResponseUtils.jsonpResponse(jsoncallback, JsonConvert.SerializeObject(captureResponse));

                            SendData(context, Encoding.ASCII.GetBytes(s));
                        }
                    }
                    else if ("upload".Equals(operation))
                    {
                        var fileName = ServiceProvider.DeviceManager.JustCapturedImage[ServiceProvider.DeviceManager.SelectedCameraDevice];
                        string id = ServiceProvider.DeviceManager.JustCapturedImageId[ServiceProvider.DeviceManager.SelectedCameraDevice];
                        string dateString = "2016-02-15-08-32-55";

                        byte[] imageBytes = File.ReadAllBytes(fileName);
                        // var s = JsonConvert.SerializeObject(ServiceProvider.Settings.DefaultSession, Formatting.Indented);
                        // byte[] imageBytes = ServiceProvider.DeviceManager.LiveViewImage[ServiceProvider.DeviceManager.SelectedCameraDevic];
                        string imageDataBase64 = Convert.ToBase64String(imageBytes);
                        int length = imageDataBase64.Length;

                        UploadResponse uploadResponse = new UploadResponse("OK", fileName, id, length, dateString, imageDataBase64);
                        var s = ResponseUtils.jsonpResponse(jsoncallback, JsonConvert.SerializeObject(uploadResponse));

                        SendData(context, Encoding.ASCII.GetBytes(s));
                    }
                    else
                    {
                        Log.Error("Unknown JSONP operation: " + operation);
                    }
                    Log.Debug("jsonp.api operation is " + operation + " - complete");
                    // return ModuleResult.Continue;
                }

                if (context.Request.Uri.AbsolutePath.StartsWith("/settings.json"))
                {
                    var s = JsonConvert.SerializeObject(ServiceProvider.Settings, Formatting.Indented);
                    SendData(context, Encoding.ASCII.GetBytes(s));
                }

                if (context.Request.Uri.AbsolutePath.StartsWith("/liveview.jpg"))
                {
                    StartLiveViewIfNeeded();
                    if (
                        ServiceProvider.DeviceManager.SelectedCameraDevice != null &&
                        ServiceProvider.DeviceManager.LiveViewImage.ContainsKey(
                        ServiceProvider.DeviceManager.SelectedCameraDevice))
                    {
                        SendDataFile(context,
                            ServiceProvider.DeviceManager.LiveViewImage[ServiceProvider.DeviceManager.SelectedCameraDevice], MimeTypeProvider.Instance.Get("liveview.jpg"));
                    }
                    else
                    {
                        Log.Debug("Could not get liveview.jpg");
                        ICameraDevice selectedCameraDevice = ServiceProvider.DeviceManager.SelectedCameraDevice;
                        Log.Debug("ServiceProvider.DeviceManager.SelectedCameraDevice is " + selectedCameraDevice);
                        string noCameraImage = Path.Combine(Settings.WebServerFolder, "img\\NoCameraDetected.png");
                        SendFile(context, noCameraImage);
                    }
                }

                if (context.Request.Uri.AbsolutePath.StartsWith("/liveviewwebcam.jpg") &&
                    ServiceProvider.DeviceManager.SelectedCameraDevice != null )
                {
                    StartLiveViewIfNeeded();
                    if (ServiceProvider.DeviceManager.LiveViewImage.ContainsKey(
                        ServiceProvider.DeviceManager.SelectedCameraDevice))
                        SendDataFile(context,
                            ServiceProvider.DeviceManager.LiveViewImage[
                                ServiceProvider.DeviceManager.SelectedCameraDevice],
                            MimeTypeProvider.Instance.Get("liveview.jpg"));
                }

                if (context.Request.Uri.AbsolutePath.StartsWith("/image/"))
                {
                    foreach (FileItem item in ServiceProvider.Settings.DefaultSession.Files)
                    {
                        if (Path.GetFileName(item.FileName) ==
                            Path.GetFileName(context.Request.Uri.AbsolutePath.Replace("/", "\\")))
                        {
                            SendFile(context, item.FileName);
                            return ModuleResult.Continue;
                        }
                    }
                }

                var slc = queryString["slc"];
                if (ServiceProvider.Settings.AllowWebserverActions && !string.IsNullOrEmpty(slc))
                {
                    string camera = queryString["camera"];
                    string[] args = new[] { queryString["slc"], queryString["param1"], queryString["param2"] };
                    string response = TakePicture(camera, args);

                    byte[] buffer = Encoding.UTF8.GetBytes(response);

                    //response.ContentLength64 = buffer.Length;
                    context.Response.AddHeader("Content-Length", buffer.Length.ToString());
                    context.Response.ContentType = "text/html";
                    context.Response.Body = new MemoryStream();
                    
                    context.Response.Body.Write(buffer, 0, buffer.Length);
                    context.Response.Body.Position = 0;
                    return ModuleResult.Continue;
                }


                string fullpath = GetFullPath(context.Request.Uri);
                if (!string.IsNullOrEmpty(fullpath) && File.Exists(fullpath))
                {
                    if (Path.GetFileName(fullpath) == "slide.html")
                    {
                        string file = File.ReadAllText(fullpath);

                        StringBuilder builder = new StringBuilder();
                        foreach (FileItem item in ServiceProvider.Settings.DefaultSession.Files)
                        {
                            string tempStr = _lineFormat.Replace("@image@",
                                "/thumb/large/" + Path.GetFileName(item.LargeThumb));
                            tempStr = tempStr.Replace("@image_thumb@",
                                "/thumb/small/" + Path.GetFileName(item.SmallThumb));
                            tempStr = tempStr.Replace("@image_url@", "/image/" + Path.GetFileName(item.FileName));
                            tempStr = tempStr.Replace("@title@", item.Name);
                            tempStr = tempStr.Replace("@desc@",
                                item.FileInfo != null ? (item.FileInfo.InfoLabel ?? "") : "");
                            builder.AppendLine(tempStr);
                        }

                        file = file.Replace("@@image_list@@", builder.ToString());

                        byte[] buffer = Encoding.UTF8.GetBytes(file);

                        //response.ContentLength64 = buffer.Length;
                        context.Response.AddHeader("Content-Length", buffer.Length.ToString());

                        context.Response.Body = new MemoryStream();

                        context.Response.Body.Write(buffer, 0, buffer.Length);
                        context.Response.Body.Position = 0;
                    }
                    else
                    {
                        SendFile(context, fullpath);
                    }
                }
                string cmd = queryString["CMD"];
                string param = queryString["PARAM"];
                if (ServiceProvider.Settings.AllowWebserverActions && !string.IsNullOrEmpty(cmd))
                    ServiceProvider.WindowsManager.ExecuteCommand(cmd, param);
            }
            catch (IOException ioe)
            {
                Log.Error("Web server IOException", ioe);
            }
            catch (Exception ex)
            {
                Log.Error("Web server Exception", ex);
            }
            return ModuleResult.Continue;
        }

        private string TakePicture(string camera, string[] args)
        {
            string response = "";
            try
            {
                var processor = new CommandLineProcessor();
                processor.SetCamera(camera);
                var resp = processor.Pharse(args);
                var list = resp as IEnumerable<string>;
                if (list != null)
                {
                    foreach (var o in list)
                    {
                        response += o + "\n";
                    }
                }
                else
                {
                    if (resp != null)
                        response = resp.ToString();
                }
            }
            catch (Exception ex)
            {
                response = ex.Message;
            }
            if (string.IsNullOrEmpty(response))
                response = "OK";

            return response;
        }

        private void SendFile(IHttpContext context, string fullpath)
        {
            if (!File.Exists(fullpath))
                return;

            string str = MimeTypeProvider.Instance.Get(fullpath);
            FileStream fileStream = new FileStream(fullpath, FileMode.Open, FileAccess.Read,
                                                   FileShare.Read | FileShare.Write);
            context.Response.AddHeader("Content-Disposition",
                                       "inline;filename=\"" + Path.GetFileName(fullpath) + "\"");
            context.Response.ContentType = str;
            context.Response.ContentLength = (int)fileStream.Length;
            context.Response.Body = fileStream;
        }

        private void SendData(IHttpContext context, byte[] data)
        {
            if (data == null)
                return;
            MemoryStream stream = new MemoryStream(data);
            //            context.Response.AddHeader("Content-Disposition",
            //"inline;filename=\"" + Path.GetFileName(fullpath) + "\"");
            context.Response.ContentType = "application/json";
            context.Response.ContentLength = data.Length;
            context.Response.Body = stream;
        }

        private void SendDataFile(IHttpContext context, byte[] data, string mimet)
        {
            if (data == null)
                return;
            MemoryStream stream = new MemoryStream(data);
            //            context.Response.AddHeader("Content-Disposition",
            //"inline;filename=\"" + Path.GetFileName(fullpath) + "\"");
            context.Response.ContentType = mimet;
            context.Response.ContentLength = data.Length;
            context.Response.Body = stream;
        }

        private string GetFullPath(Uri uri)
        {
            // check first if there a branded version of the web server folder
            string file = Path.Combine(Settings.BrandingWebServerFolder,
                Uri.UnescapeDataString(uri.AbsolutePath.Remove(0, 1)).TrimStart(new[] { '/' })
                    .Replace('/', '\\'));
            if (File.Exists(file))
                return file;
            return Path.Combine(Settings.WebServerFolder,
                Uri.UnescapeDataString(uri.AbsolutePath.Remove(0, 1)).TrimStart(new[] {'/'})
                    .Replace('/', '\\'));
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void StartLiveViewIfNeeded()
        {
            if (_liveViewFirstRun)
            {
                ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.LiveViewWnd_Show);
                Thread.Sleep(500);
                ServiceProvider.WindowsManager.ExecuteCommand(CmdConsts.All_Minimize);
                // ServiceProvider.WindowsManager.ExecuteCommand(CmdConsts.LiveView_NoProcess); // turns off focus - cth
                _liveViewFirstRun = false;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void StopLiveViewIfNeeded()
        {
            if (!_liveViewFirstRun)
            {
                ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.LiveViewWnd_Hide);
                Thread.Sleep(500);
                _liveViewFirstRun = true;
            }
        }

    }
}