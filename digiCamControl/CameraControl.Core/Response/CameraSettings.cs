using CameraControl.Devices;
using CameraControl.Devices.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraControl.Core.Response
{
    class CameraSettings
    {
        public CameraSettings(ICameraDevice device)
        {
            FNumber = device.FNumber.Value;
            IsoNumber = device.IsoNumber.Value;
            ShutterSpeed = device.ShutterSpeed.Value;
            WhiteBalance = device.WhiteBalance.Value;
            Mode = device.Mode.Value;
            ExposureCompensation = device.ExposureCompensation.Value;
            CompressionSetting = device.CompressionSetting.Value;
            ExposureMeteringMode = device.ExposureMeteringMode.Value;
            FocusMode = device.FocusMode.Value;
            LiveViewImageZoomRatio = device.LiveViewImageZoomRatio.Value;
            Manufacturer = device.Manufacturer;
            exposureStatus = device.ExposureStatus;
        }

        public string FNumber { get; set; }
        public string IsoNumber { get; set; }
        public string ShutterSpeed { get; set; }
        public string WhiteBalance { get; set; }
        public string Mode { get; set; }
        public string ExposureCompensation { get; set; }
        public string CompressionSetting { get; set; }
        public string ExposureMeteringMode { get; set; }
        public string FocusMode { get; set; }
        public string LiveViewImageZoomRatio { get; set; }
        public string Manufacturer { get; set; }
        int exposureStatus { get; set; }
    }
}
