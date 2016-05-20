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
        private static ICameraDevice _captureDevice = null;

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static JustCaptured getJustCaptured() {
            JustCaptured justCaptured = null;
            ICameraDevice device = _captureDevice;
            if (device != null)
            {
                if (!ServiceProvider.DeviceManager.JustCaptured.ContainsKey(device))
                {
                    ServiceProvider.DeviceManager.JustCaptured[device] = new JustCaptured();
                }
                justCaptured = ServiceProvider.DeviceManager.JustCaptured[device];
            }
            return justCaptured;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static string startCapture()
        {
            string id = null;
            _captureDevice = CameraHelper.GetSelectedCameraDevice();
            if (_captureDevice != null)
            {
                id = Guid.NewGuid().ToString("N");
                setId(id);
                setCaptureState(JustCaptured.CaptureState.GETTING_PREVIEW);
                _previewSemaphore = new Semaphore(0, 1);
                _photoSemaphore = new Semaphore(0, 1);
            }
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
        public static bool isExpectedId(string id, string context)
        {
            string expectedId = getId();
            if (id != null && id.Equals(expectedId))
            {
                return true;
            }
         else
            {
                Log.Debug("CapturedHelper " + context + ": current expected ID " + expectedId + " differs from ID " + id);
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void cancelCapture(string id)
        {
            if (id != null)
            {
                if (isExpectedId(id, "cancelCapture"))
                {
                    setCaptureState(JustCaptured.CaptureState.CANCELLED);
                    _captureDevice = null;
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static string getImageFilename(string id)
        {
            string imageFilename = null;
            if (isExpectedId(id, "getImageFilename"))
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
            if (isExpectedId(id, "getPreviewFilename"))
            {
                previewFilename = getJustCaptured().PreviewFilename;
            }
            return previewFilename;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static OrientationType getPhotoOrientation(string id)
        {
            OrientationType orientationValue = OrientationType.Undefined;

            if (isExpectedId(id, "getPhotoOrientation"))
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
            if (isExpectedId(id, "WaitPreviewReady"))
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
            if (isExpectedId(id, "WaitPhotoReady"))
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
