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
using ImageMagick;
using CameraControl.Devices.Classes;

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
                // Avoid remove control/monitoring of the camera for security purposes
                if (!"localhost".Equals(context.Request.Uri.Host))
                {
                    context.Response.StatusCode = 403;
                    context.Response.ReasonPhrase = "Forbidden";
                    return ModuleResult.Continue;
                }

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

                if (context.Request.Uri.AbsolutePath.StartsWith("/thumb/captured/"))
                {
                    string id = getDelimitedId(context.Request.Uri.AbsolutePath, "/", ".");
                    string quickThumb = CapturedHelper.getPreviewFilename(id);
                    if (quickThumb == null || !File.Exists(quickThumb))
                    {
                        quickThumb = Path.Combine(Settings.WebServerFolder, "img\\PreviewNotFound.png");
                    }
                    SendFile(context, quickThumb);
                    return ModuleResult.Continue;
                }

                if (context.Request.Uri.AbsolutePath.StartsWith("/jsonp.api"))
                {
                    var operation = queryString["operation"];

                    // someCallBackString({ The Object });
                    var jsoncallback = queryString["jsoncallback"];

                    Log.Debug("== jsonp.api operation is " + operation + " - started");
                    if ("capture".Equals(operation))
                    {
                        string response = "undefined";
                        string id = null;
                        string subjectAlias = queryString["subjectAlias"];
                        string subjectEmployeeId = queryString["subjectEmployeeId"];
                        Log.Debug("Capturing photo for subject with alias " + subjectAlias + " and employee ID " + subjectEmployeeId);

                        if (CameraHelper.GetSelectedCameraDevice() == null)
                        {
                            response = "Camera Disconnected";
                        }
                        else {
                            // Mark any previous images for deletion
                            foreach (FileItem item in ServiceProvider.Settings.DefaultSession.Files)
                            {
                                item.IsChecked = true;
                            }

                            id = CapturedHelper.startCapture(subjectEmployeeId, subjectAlias);
                            Log.Debug("-- jsonp.api id is " + id);

                            string camera = queryString["camera"];
                            string param1 = queryString["param1"] ?? "";
                            string param2 = queryString["param2"] ?? "";
                            string[] args = new[] { operation, param1, param2 };
                            response = TakePicture(camera, args);
                        }
                        Log.Debug("TakePicture respose is: " + response);
                        if (response != "OK" || id == null)
                        {
                            Log.Debug("Sending error response: " + response);
                            CapturedHelper.cancelCapture(id);

                            CaptureResponse captureResponse = new CaptureResponse(response);
                            var s = ResponseUtils.jsonpResponse(jsoncallback, JsonConvert.SerializeObject(captureResponse));

                            SendData(context, Encoding.ASCII.GetBytes(s));

                            Log.Debug("Reseting LiveView due to error: " + response);
                            if (CameraHelper.GetSelectedCameraDevice() != null)
                            {
                                // This can help fix focus errors
                                StopLiveViewIfNeeded(true);
                                StartLiveViewIfNeeded(true);
                            }
                        }
                        else
                        {
                            CapturedHelper.WaitPreviewReady(id);

                            var fileName = CapturedHelper.getImageFilename(id);

                            // Remove any previous image
                            ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.Del_Image, true);
                            Log.Debug("Called ExecuteCommand with WindowsCmdConsts.Del_Image");

                            CaptureResponse captureResponse = new CaptureResponse(response, fileName, id);
                            var s = ResponseUtils.jsonpResponse(jsoncallback, JsonConvert.SerializeObject(captureResponse));

                            SendData(context, Encoding.ASCII.GetBytes(s));
                        }
                    }
                    else if ("upload".Equals(operation))
                    {
                        var id = queryString["storageKey"];
                        Log.Debug("-- jsonp.api id is " + id);

                        UploadResponse uploadResponse = null;
                        if (!CapturedHelper.isExpectedId(id, "upload operation"))
                        {
                            uploadResponse = new UploadResponse("Upload called with invalid storage key");
                        }
                        else
                        {
                            CapturedHelper.WaitPhotoReady(id);

                            var fileName = CapturedHelper.getImageFilename(id);

                            // Get an ISO 8601 formatted date string
                            string dateString = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture);

                            OrientationType orientation = CapturedHelper.getPhotoOrientation(id);
                            byte[] imageBytes = File.ReadAllBytes(fileName);
                            // var s = JsonConvert.SerializeObject(ServiceProvider.Settings.DefaultSession, Formatting.Indented);
                            string imageDataBase64 = Convert.ToBase64String(imageBytes);
                            int length = imageDataBase64.Length;

                            CameraInfo cameraInfo = new CameraInfo(CameraHelper.GetSelectedCameraDevice());

                            string version = ServiceProvider.Settings.CurrentSoftwareVersion();
                            uploadResponse = new UploadResponse("OK", version, fileName, id, length, dateString, orientation, cameraInfo, imageDataBase64);
                        }

                        var s = ResponseUtils.jsonpResponse(jsoncallback, JsonConvert.SerializeObject(uploadResponse));
                        // We need UTF-8 for things like "f/5.6"
                        SendData(context, Encoding.UTF8.GetBytes(s));
                    }
                    else if ("status".Equals(operation))
                    {
                        // Get an ISO 8601 formatted date string
                        string dateString = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture);

                        CameraInfo cameraInfo = new CameraInfo(CameraHelper.GetSelectedCameraDevice());

                        string version = ServiceProvider.Settings.CurrentSoftwareVersion();
                        StatusResponse statusResponse = new StatusResponse("OK", version, dateString, cameraInfo);
                        var s = ResponseUtils.jsonpResponse(jsoncallback, JsonConvert.SerializeObject(statusResponse));

                        SendData(context, Encoding.UTF8.GetBytes(s));
                    }
                    else if ("liveview".Equals(operation))
                    {
                        string responseMessage = "OK";
                        var command = queryString["command"];
                        try
                        {
                            if ("show".Equals(command))
                            {
                                StartLiveViewIfNeeded(true);
                            }
                            else if ("hide".Equals(command))
                            {
                                StopLiveViewIfNeeded(true);
                            }
                            else
                            {
                                responseMessage = string.Format("Error duing JSONP operation {0}: unknown command: {1}", operation, command);
                                Log.Error(responseMessage);
                            }
                        }
                        catch (Exception e)
                        {
                            responseMessage = string.Format("Exception during JSONP operation {0}: {1}", operation, e);
                            Log.Error(responseMessage);
                        }

                        StatusMessage statusMessage = ResponseUtils.createStatusMessage(responseMessage);
                        var s = ResponseUtils.jsonpResponse(jsoncallback, JsonConvert.SerializeObject(statusMessage));

                        SendData(context, Encoding.UTF8.GetBytes(s));
                    }
                    else
                    {
                        Log.Error("Unknown JSONP operation: " + operation);
                    }
                    Log.Debug("== jsonp.api operation is " + operation + " - complete");
                    // return ModuleResult.Continue;
                }

                if (context.Request.Uri.AbsolutePath.StartsWith("/settings.json"))
                {
                    var s = JsonConvert.SerializeObject(ServiceProvider.Settings, Formatting.Indented);
                    SendData(context, Encoding.ASCII.GetBytes(s));
                }

                if (context.Request.Uri.AbsolutePath.StartsWith("/liveview.jpg"))
                {
                    StartLiveViewIfNeeded(false);
                    ICameraDevice device = CameraHelper.GetSelectedCameraDevice();
                    if (
                        device != null &&
                        ServiceProvider.DeviceManager.LiveViewImage.ContainsKey(device))
                    {
                        SendDataFile(context,
                            ServiceProvider.DeviceManager.LiveViewImage[device], MimeTypeProvider.Instance.Get("liveview.jpg"));
                    }
                    else
                    {
                        Log.Debug("Could not get liveview.jpg");
                        Log.Debug("ServiceProvider.DeviceManager.SelectedCameraDevice is " + device);
                        string noCameraImage = Path.Combine(Settings.WebServerFolder, "img\\NoCameraDetected.png");
                        SendFile(context, noCameraImage);
                    }
                }

                if (context.Request.Uri.AbsolutePath.StartsWith("/liveviewwebcam.jpg") &&
                    CameraHelper.GetSelectedCameraDevice() != null)
                {
                    StartLiveViewIfNeeded(false);
                    ICameraDevice device = CameraHelper.GetSelectedCameraDevice();
                    if (ServiceProvider.DeviceManager.LiveViewImage.ContainsKey(device))
                        SendDataFile(context,
                            ServiceProvider.DeviceManager.LiveViewImage[device], MimeTypeProvider.Instance.Get("liveviewwebcam.jpg"));
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

        private string getDelimitedId(string uri, string beginStr, string lastStr)
        {
            string id = null;

            if (uri != null)
            {
                int beginIdx = uri.LastIndexOf(beginStr) + 1;
                int endIdx = uri.LastIndexOf(lastStr);
                if (beginIdx > 0 && endIdx > beginIdx)
                {
                    id = uri.Substring(beginIdx, endIdx - beginIdx);
                }
            }

            return id;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void StartLiveViewIfNeeded(bool needed)
        {
            if (_liveViewFirstRun || needed)
            {
                ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.LiveViewWnd_Show);
                Thread.Sleep(1); // was 500
                ServiceProvider.WindowsManager.ExecuteCommand(CmdConsts.All_Minimize);
                // ServiceProvider.WindowsManager.ExecuteCommand(CmdConsts.LiveView_NoProcess); // turns off focus - cth
                _liveViewFirstRun = false;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void StopLiveViewIfNeeded(bool needed)
        {
            if (!_liveViewFirstRun || needed)
            {
                ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.LiveViewWnd_Hide);
                Thread.Sleep(500);
                _liveViewFirstRun = true;
            }
        }

    }
}