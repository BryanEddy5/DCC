using CameraControl.Core.Classes;
using CameraControl.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraControl.Core.Response
{
    class CameraInfo
    {
        public CameraInfo(ICameraDevice device)
        {
            if (device != null && device.SerialNumber == null)
            {
                device = null; // no real camera connected
            }
            if (device != null)
            {
                CameraProperty property = ServiceProvider.DeviceManager.SelectedCameraDevice.LoadProperties();

                cameraSettings = new CameraSettings(device);
                softwareSettings = new SoftwareSettings(property);

                cameraName = device.DeviceName;
                cameraSerialNumber = device.SerialNumber;
            }
        }

        public string cameraName { get; set; }
        public string cameraSerialNumber { get; set; }
        public CameraSettings cameraSettings { get; set; }
        public SoftwareSettings softwareSettings { get; set; }
    }
}
