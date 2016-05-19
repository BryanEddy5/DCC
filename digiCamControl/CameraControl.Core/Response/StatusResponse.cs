using ImageMagick;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraControl.Core.Response
{
    class StatusResponse
    {
        public StatusResponse(string response, string version, string dateString,
            string cameraName, string cameraSerialNumber, CameraSettings cameraSettings, SoftwareSettings softwareSettings)
        {
            this.response = response;
            this.version = version;
            this.dateString = dateString;
            this.cameraName = cameraName;
            this.cameraSerialNumber = cameraSerialNumber;
            this.cameraSettings = cameraSettings;
            this.softwareSettings = softwareSettings;
            this.statusMessage = ResponseUtils.createStatusMessage(response);
        }

        public string response { get; set; }
        public string version { get; set; }
        public StatusMessage statusMessage { get; set; }
        public string dateString { get; set; }
        public string cameraName { get; set; }
        public string cameraSerialNumber { get; set; }
        public CameraSettings cameraSettings { get; set; }
        public SoftwareSettings softwareSettings { get; set; }
    }
}
