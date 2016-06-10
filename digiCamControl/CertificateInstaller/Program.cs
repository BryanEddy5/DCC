using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace CertificateInstaller
{
    class Program
    {
        static string logFile = @"C:\temp\certificate\tmp2.log";
        static StreamWriter logStream = null;

        static void Main(string[] args)
        {
            try
            {
                beginLogging();
                DebugWriteLine("==Hello from the CertificateInstaller at " + DateTime.Now.ToString());

                string cwd = Directory.GetCurrentDirectory();
                DebugWriteLine("cwd: " + cwd);
                string exeDir = GetExecutingDirectoryName();
                DebugWriteLine("exeDir: " + exeDir);

                string issuedBy = "CN=AmazonWebCat"; // default
                string certFile = null; // e.g., @"C:\temp\certificate\AmazonWebCatBundle.pfx";
                string password = null;

                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    if ("-p".Equals(arg))
                    {
                        password = args[++i];
                    }
                    else if ("-importpfx".Equals(arg))
                    {
                        certFile = args[++i];
                    }
                    else if ("-issuedBy".Equals(arg))
                    {
                        issuedBy = args[++i];
                    }
                    else
                    {
                        DebugWriteLine(String.Format("CertificateInstaller called with invalid argument ({0})", arg));
                    }
                }

                // Make sure the path is absolute exeDir
                if (certFile != null)
                {
                    certFile = Path.GetFullPath(Path.Combine(exeDir, certFile));
                }
                DebugWriteLine(String.Format("certFile: {0}", certFile));
                DebugWriteLine(String.Format("issuedBy: {0}", issuedBy));

                X509Store store = getStore();
                X509Certificate2 certificate = GetCertificateFromStore(store, issuedBy);
                if (password != null && certFile != null)
                {
                    DebugWriteLine("==Making X509Certificate2 object");
                    certificate = new X509Certificate2(certFile, password);
                    DebugWriteLine("==Adding the certificate to the store");
                    AddPfxFileToStore(certFile, password);
                }

                DebugWriteLine("==Getting the certificate");
                certificate = GetCertificateFromStore(store, issuedBy);

                if (certificate != null)
                {
                    DebugWriteLine("==Updating permissions on certificate");
                    UpdatePermissions(certificate);
                }
                else
                {
                    DebugWriteLine(String.Format("Certificate not found: issuedBy is {0}", issuedBy));
                }
            }
            catch (Exception ex)
            {
                DebugWriteLine("Exception during processing: " + ex);
            }
            finally
            {
                DebugWriteLine("==Goodbye from the CertificateInstaller");
                endLogging();
            }
        }

        private static string GetExecutingDirectoryName() {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase );
            return path;
        }

        private static void beginLogging()
        {
            if (logFile != null)
            {
                logStream = new StreamWriter(logFile);
            }
        }

        private static void endLogging()
        {
            if (logStream != null)
            {
                logStream.Flush();
                logStream.Close();
                logStream.Dispose();
                logStream = null;
            }

        }

        private static void DebugWriteLine(string message) {
            if (logStream != null)
            {
                logStream.WriteLine(message);
            }
            Debug.WriteLine(message);
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

        private static bool AddPfxFileToStore(string pfxFile, string password)
        {
            int exitCode = -1;
            try
            {
                Process process = new Process();
                process.StartInfo.UseShellExecute = false;
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                string systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
                startInfo.FileName = Path.Combine(systemFolder, "certutil.exe"); // i.e., @"C:\WINDOWS\system32\certutil.exe";
                startInfo.Arguments = String.Format("-p \"{0}\" -importpfx \"{1}\"", password, pfxFile);
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                exitCode = process.ExitCode;
            }
            catch (Exception ex)
            {
                DebugWriteLine("AddPfxFileToStore failed: " + ex);
            }

            DebugWriteLine(String.Format("AddPfxFileToStore exit code is {0}", exitCode));
            return exitCode == 0;
        }

        // This should replace AddPfxFileToStore so we don't need to create a process or rely on an executable.
        // This does not yet work correctly.
        // The private key has incorrect permissions to that even UpdatePermissions does not work.
        // Possibly solvable by looking at the following:
        // http://stackoverflow.com/questions/12106011/system-security-cryptography-cryptographicexception-keyset-does-not-exist
        private static void AddCertificateToStore(X509Store store, X509Certificate2 cert)
        {
            try
            {
                store.Open(OpenFlags.ReadWrite);
                if (!store.Certificates.Contains(cert))
                {
                    // store.Add(cert); // AddRange to persist?
                    X509Certificate2Collection certs = new X509Certificate2Collection();
                    certs.Add(cert);
                    store.AddRange(certs);
                }
            }
            catch (Exception ex)
            {
                DebugWriteLine("AddCertificateToStore failed: " + ex);
            }
            finally
            {
                store.Close();
            }
        }

        private static void UpdatePermissions(X509Certificate2 certificate)
        {
            try
            {
                var rsa = certificate.PrivateKey as RSACryptoServiceProvider;
                if (rsa != null)
                {
                    // Modifying the CryptoKeySecurity of a new CspParameters and then instantiating
                    // a new RSACryptoServiceProvider seems to be the trick to persist the access rule.
                    // cf. http://blogs.msdn.com/b/cagatay/archive/2009/02/08/removing-acls-from-csp-key-containers.aspx
                    var cspParams = new CspParameters(rsa.CspKeyContainerInfo.ProviderType, rsa.CspKeyContainerInfo.ProviderName, rsa.CspKeyContainerInfo.KeyContainerName)
                    {
                        Flags = CspProviderFlags.UseExistingKey | CspProviderFlags.UseMachineKeyStore,
                        CryptoKeySecurity = rsa.CspKeyContainerInfo.CryptoKeySecurity
                    };

                    //var sid = "Authenticated Users";
                    SecurityIdentifier authenticatedUsers = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
                    cspParams.CryptoKeySecurity.AddAccessRule(new CryptoKeyAccessRule(authenticatedUsers, CryptoKeyRights.GenericRead, AccessControlType.Allow));

                    using (var rsa2 = new RSACryptoServiceProvider(cspParams))
                    {
                        // Only created to persist the rule change in the CryptoKeySecurity
                    }
                }
            }
            catch (Exception ex)
            {
                DebugWriteLine("Update Permissions for certificate failed: " + ex);
            }
        }

    }
}
