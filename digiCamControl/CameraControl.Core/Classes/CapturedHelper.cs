using CameraControl.Devices;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CameraControl.Core.Classes
{
    public class CapturedHelper
    {
        private static Semaphore _previewSemaphore = new Semaphore(0, 1);
        private static Semaphore _photoSemaphore = new Semaphore(0, 1);

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static JustCaptured getJustCaptured() {
            if (!ServiceProvider.DeviceManager.JustCaptured.ContainsKey(ServiceProvider.DeviceManager.SelectedCameraDevice))
            {
                ServiceProvider.DeviceManager.JustCaptured[ServiceProvider.DeviceManager.SelectedCameraDevice] = new JustCaptured();
            }
            JustCaptured justCaptured = ServiceProvider.DeviceManager.JustCaptured[ServiceProvider.DeviceManager.SelectedCameraDevice];
            return justCaptured;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static string startCapture()
        {
            string id = Guid.NewGuid().ToString("N");
            setId(id);
            setCaptureState(JustCaptured.CaptureState.GETTING_PREVIEW);
            _previewSemaphore = new Semaphore(0, 1);
            _photoSemaphore = new Semaphore(0, 1);
            return id;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static string getId()
        {
            string id = getJustCaptured().ImageId;
            return id;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void setId(string id)
        {
            getJustCaptured().ImageId = id;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static JustCaptured.CaptureState getCaptureState(string context)
        {
            JustCaptured.CaptureState captureState = getJustCaptured().State;
            Log.Debug("CapturedHelper " + context + ": captureState is " + captureState.ToString());
            return captureState;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void setCaptureState(JustCaptured.CaptureState captureState)
        {
            getJustCaptured().State = captureState;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static bool expectedId(string expectedId, string context)
        {
            string actualId = getId();
            if (expectedId != null && expectedId.Equals(actualId))
            {
                return true;
            }
            else if (expectedId != null && expectedId == "") {
                Log.Debug("CapturedHelper " + context + ": expected ID was legacy empty string, but actual ID was " + actualId);
                return true;
            }
            else
            {
                Log.Debug("CapturedHelper " + context + ": expected ID " + expectedId + ", but actual ID was " + actualId);
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void cancelCapture(string id)
        {
            if (id != null)
            {
                if (expectedId(id, "cancelCapture"))
                {
                    setCaptureState(JustCaptured.CaptureState.CANCELLED);
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static string getImageFilename(string id)
        {
            string imageFilename = null;
            if (expectedId(id, "getImageFilename"))
            {
                imageFilename = getJustCaptured().ImageFilename;
            }
            return imageFilename;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void setImageFilename(string filename)
        {
            getJustCaptured().ImageFilename = filename;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static string getPreviewFilename(string id)
        {
            string previewFilename = null;
            if (expectedId(id, "getPreviewFilename"))
            {
                previewFilename = getJustCaptured().PreviewFilename;
            }
            return previewFilename;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static OrientationType getPhotoOrientation(string id)
        {
            OrientationType orientationValue = OrientationType.Undefined;

            if (expectedId(id, "getPhotoOrientation"))
            {
                string orientation = getJustCaptured().Orientation;
                orientationValue = (OrientationType)Enum.Parse(typeof(OrientationType), orientation);
            }
            return orientationValue;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void setPhotoOrientation(OrientationType orientationValue)
        {
            string orientation = orientationValue.ToString();
            getJustCaptured().Orientation = orientation;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void setPreviewFilename(string previewFilename)
        {
            getJustCaptured().PreviewFilename = previewFilename;
        }

        // Do not synchronize this method
        public static void WaitPreviewReady(string id)
        {
            getCaptureState("WaitPreviewReady");
            if (expectedId(id, "WaitPreviewReady"))
            {
                _previewSemaphore.WaitOne();
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void SignalPreviewReady()
        {
            JustCaptured.CaptureState captureState = getCaptureState("SignalPreviewReady");
            setCaptureState(JustCaptured.CaptureState.GETTING_PHOTO);
            _previewSemaphore.Release();
        }

        // Do not synchronize this method
        public static void WaitPhotoReady(string id)
        {
            getCaptureState("WaitPhotoReady");
            if (expectedId(id, "WaitPhotoReady"))
            {
                _photoSemaphore.WaitOne();
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void SignalPhotoReady()
        {
            JustCaptured.CaptureState captureState = getCaptureState("SignalPhotoReady");
            setCaptureState(JustCaptured.CaptureState.COMPLETE);
            _photoSemaphore.Release();
        }

    }
}
