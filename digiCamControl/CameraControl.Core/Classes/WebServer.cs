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
using System.Linq;
using System.Net;
using System.Text;
using CameraControl.Devices;
// using Griffin.Networking.Protocol.Http.Services.BodyDecoders;
using Griffin.WebServer;
using Griffin.WebServer.Files;
using Griffin.WebServer.Modules;
using Griffin.WebServer.Routing;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using CameraControl.Core.Response;
using System.Security.Cryptography;
using System.Security.AccessControl;

#endregion

namespace CameraControl.Core.Classes
{
    public class WebServer
    {
        public void Start(int port)
        {
            try
            {
                // Module manager handles all modules in the server
                var moduleManager = new ModuleManager();

                // Let's serve our downloaded files (Windows 7 users)
                var fileService = new DiskFileService("/", Settings.WebServerFolder);

                // Create the file module and allow files to be listed.
                var module = new FileModule(fileService) { AllowFileListing = false };

                var routerModule = new RouterModule();

                // Add the module
                //moduleManager.Add(module);
                moduleManager.Add(new WebServerModule());

                //moduleManager.Add(new BodyDecodingModule(new UrlFormattedDecoder()));

                string issuedBy = "CN=AmazonWebCat";
                X509Store store = getStore();
                X509Certificate2 certificate = GetCertificateFromStore(store, issuedBy);
                if (certificate == null)
                {
                    Log.Error(String.Format("Could not get certificate issued by {0}", issuedBy));
                    // Use legacy certificate for one version iteration
                    var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    // from Griffin.Framework\samples\Networking\CustomProtocol\DemoTest\DemoTest\bin\Debug\Net\cert
                    certificate = new X509Certificate2(baseDirectory + "\\Net\\cert\\AmazonWebCatBundle.pfx", "webcat");
                }

                // Make sure things are loaded for serialization
                JsonConvert.SerializeObject(new CaptureResponse("OK"));

                // And start the server.
                var server = new HttpServer(moduleManager);

                // server.Start(IPAddress.Any, port);
                server.Start(IPAddress.Any, port, certificate);
            }
            catch (Exception ex)
            {
                Log.Error("Unable to start web server ", ex);
            }
        }

        public void Stop()
        {
        }

        private static X509Store getStore()
        {
            // Get from "Personal"
            X509Store store = new X509Store(StoreLocation.LocalMachine);
            // Get from "Trusted Root Certification Authorities"
            // X509Store store = new X509Store(StoreName.Root);
            return store;
        }

        private static X509Certificate2 GetCertificateFromStore(X509Store store, string certName)
        {
            X509Certificate2 cert = null;
            try
            {
                store.Open(OpenFlags.ReadOnly);

                // Place all certificates in an X509Certificate2Collection object.
                X509Certificate2Collection certCollection = store.Certificates;

                // If using a certificate with a trusted root you do not need to FindByTimeValid
                X509Certificate2Collection currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                X509Certificate2Collection signingCert = currentCerts.Find(X509FindType.FindByIssuerDistinguishedName, certName, false);
                if (signingCert.Count > 0)
                {
                    // Use the first certificate in the collection, has the right name and is current.
                    cert = signingCert[0];
                }
            }
            finally
            {
                store.Close();
            }

            return cert;
        }

    }
}