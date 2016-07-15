using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.IO;

namespace TestImageReadWrite
{
    class TestImageReadWrite
    {
        static void Main(string[] args)
        {
            int iterations = 20;
            testPerformanceFromStream(@"c:\temp\benchmarkIn.jpg", @"c:\temp\benchmarkOut.jpg", iterations);
            testPerformanceFromFile(@"c:\temp\benchmarkIn.jpg", @"c:\temp\benchmarkOut.jpg", iterations);

            testPerformanceFromStream(@"c:\temp\benchmarkIn.jpg", @"c:\temp\benchmarkOut.jpg", iterations); 
            testPerformanceFromFile(@"c:\temp\benchmarkIn.jpg", @"c:\temp\benchmarkOut.jpg", iterations);

        }
        
        static void testPerformanceFromFile(string inputFile, string outputFile, int iterations)
        {
            // benchmark test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var watch = System.Diagnostics.Stopwatch.StartNew();
            var beforeVirt = System.Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64;
            var beforeWorking = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;

            for (int i = 0; i < iterations; i++)
            {
                // performs operations here
                try
                {
                    //using (FileStream fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
                    {
                        //using (Image image = Image.FromStream(fs))
                        using (Image image = Image.FromFile(inputFile))
                        {
                            ImageFormat sourceFormat = image.RawFormat;
                            EncoderParameters encoderParams = null;
                            try
                            {

                                // The following rotates a JPEG losslessly for dimensions of multiples of 8 and rotations of multiples of 90 degress
                                if (sourceFormat.Guid == ImageFormat.Jpeg.Guid)
                                {
                                    encoderParams = new EncoderParameters(1);
                                    encoderParams.Param[0] = new EncoderParameter(Encoder.Transformation,
                                        (long)EncoderValue.TransformRotate270);
                                }
                                //PhotoUtils.CreateFolder(outputFile);

                                ImageCodecInfo encoder = null;
                                foreach (var info in ImageCodecInfo.GetImageEncoders())
                                {
                                    if (info.FormatID == sourceFormat.Guid)
                                    {
                                        encoder = info;
                                        break;
                                    }
                                }

                                image.Save(outputFile, encoder, encoderParams);
                                //PhotoUtils.WaitForFile(outputFile);
                            }
                            finally
                            {
                                if (encoderParams != null)
                                    encoderParams.Dispose();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception during test: " + e);
                }
                Debug.WriteLine(string.Format("Iteration complete: " + i));
            }

            var afterVirt = System.Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64;
            var afterWorking = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
            Debug.WriteLine(string.Format("FromFile difference for VirtualMemorySize64 is {0}", afterVirt - beforeVirt));
            Debug.WriteLine(string.Format("FromFile difference for WorkingSet64 is {0}", afterWorking - beforeWorking));

            watch.Stop();
            Debug.WriteLine("ms for test " + watch.ElapsedMilliseconds);
            Debug.WriteLine("test complete");
        }

        static void testPerformanceFromStream(string inputFile, string outputFile, int iterations)
        {
            // benchmark test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var watch = System.Diagnostics.Stopwatch.StartNew();
            var beforeVirt = System.Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64;
            var beforeWorking = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;

            for (int i = 0; i < iterations; i++)
            {
                // performs operations here
                try
                {
                    using (FileStream fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
                    {
                        using (Image image = Image.FromStream(fs))
                        //using (Image image = Image.FromFile(inputFile))
                        {
                            ImageFormat sourceFormat = image.RawFormat;
                            EncoderParameters encoderParams = null;
                            try
                            {

                                // The following rotates a JPEG losslessly for dimensions of multiples of 8 and rotations of multiples of 90 degress
                                if (sourceFormat.Guid == ImageFormat.Jpeg.Guid)
                                {
                                    encoderParams = new EncoderParameters(1);
                                    encoderParams.Param[0] = new EncoderParameter(Encoder.Transformation,
                                        (long)EncoderValue.TransformRotate270);
                                }
                                //PhotoUtils.CreateFolder(outputFile);

                                ImageCodecInfo encoder = null;
                                foreach (var info in ImageCodecInfo.GetImageEncoders())
                                {
                                    if (info.FormatID == sourceFormat.Guid)
                                    {
                                        encoder = info;
                                        break;
                                    }
                                }

                                image.Save(outputFile, encoder, encoderParams);
                                //PhotoUtils.WaitForFile(outputFile);
                            }
                            finally
                            {
                                if (encoderParams != null)
                                    encoderParams.Dispose();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception during test: " + e);
                }
                Debug.WriteLine(string.Format("Iteration complete: " + i));
            }

            var afterVirt = System.Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64;
            var afterWorking = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
            Debug.WriteLine(string.Format("FromStream difference for VirtualMemorySize64 is {0}", afterVirt - beforeVirt));
            Debug.WriteLine(string.Format("FromStream difference for WorkingSet64 is {0}", afterWorking - beforeWorking));

            watch.Stop();
            Debug.WriteLine("ms for test " + watch.ElapsedMilliseconds);
            Debug.WriteLine("test complete");
        }

    }
}
