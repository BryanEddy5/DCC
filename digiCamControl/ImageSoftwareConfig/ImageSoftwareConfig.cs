using ImageMagick;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageSoftwareConfig
{
    // Create the machine-specific ImageMagick folder so that it can be used for all users.
    // The default location is %LOCALAPPDATA%/ImageMagick.
    // Otherwise, the first picture takes about 40 seconds for each user on a given machine.
    class ImageSoftwareConfig
    {
        static void Main(string[] args)
        {
            // defaults
            string imageSoftwareName = "ImageMagick";
            string appName = "digiCamControl";

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if ("--imageSoftwareName".Equals(arg))
                {
                    imageSoftwareName = args[++i];
                }
                else if ("--appName".Equals(arg))
                {
                    appName = args[++i];
                }
            }

            string localImageSoftwareDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), imageSoftwareName);
            string commonImageSoftwareDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), appName, imageSoftwareName);

            new ImageSoftwareConfig().run(localImageSoftwareDir, commonImageSoftwareDir);
        }

        ImageSoftwareConfig()
        {

        }

        public void run(string localImageMagickDir, string commonImageMagickDir)
        {
            // Test that we're loaded and ready - this populates localImageMagickDir if needed
            MagickImage image = new MagickImage(new MagickColor(Color.Black), 100, 200);
            image.Thumbnail(50, 100);

            if (Directory.Exists(localImageMagickDir))
            {
                if (Directory.Exists(commonImageMagickDir))
                {
                    // Make sure we get a fresh copy in case there have been updates
                    Directory.Delete(commonImageMagickDir, true);
                }
                if (!Directory.Exists(commonImageMagickDir))
                {
                    Directory.CreateDirectory(commonImageMagickDir);
                }

                DirectoryCopy(localImageMagickDir, commonImageMagickDir, true);
            }
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
    }
}
