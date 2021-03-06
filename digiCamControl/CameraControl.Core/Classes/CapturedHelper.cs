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
        // Use a maxCount > 1 to allow multiple retrivals of the same preview or photo
        private static int maxCount = Int32.MaxValue;
        private static Semaphore _previewSemaphore = new Semaphore(0, maxCount);
        private static Semaphore _photoSemaphore = new Semaphore(0, maxCount);
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
        public static string startCapture(string subjectEmployeeId, string subjectAlias)
        {
            string id = null;
            _captureDevice = CameraHelper.GetSelectedCameraDevice();
            if (_captureDevice != null)
            {
                id = Guid.NewGuid().ToString("N");
                setId(id);
                setSubjectEmployeeId(subjectEmployeeId);
                setSubjectAlias(subjectAlias);
                setCaptureState(JustCaptured.CaptureState.GETTING_PREVIEW);
                _previewSemaphore = new Semaphore(0, maxCount);
                _photoSemaphore = new Semaphore(0, maxCount);
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
        public static string getSubjectEmployeeId()
        {
            string subjectEmployeeId = getJustCaptured().SubjectEmployeeId;
            return subjectEmployeeId;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void setSubjectEmployeeId(string subjectEmployeeId)
        {
            getJustCaptured().SubjectEmployeeId = subjectEmployeeId;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static string getSubjectAlias()
        {
            string subjectAlias = getJustCaptured().SubjectAlias;
            return subjectAlias;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void setSubjectAlias(string subjectAlias)
        {
            getJustCaptured().SubjectAlias = subjectAlias;
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
            try
            {
                _previewSemaphore.Release(maxCount);
            }
            catch (Exception e)
            {
                Log.Debug("Exception in SignalPreviewReady(): " + e.Message);
            }
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
            try
            {
                _photoSemaphore.Release(maxCount);
            }
            catch (Exception e)
            {
                Log.Debug("Exception in SignalPhotoReady(): " + e.Message);
            }
        }

    }
}
