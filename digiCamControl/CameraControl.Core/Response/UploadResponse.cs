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
    class UploadResponse
    {
        public UploadResponse(string response)
        {
            this.response = response;
            this.statusMessage = ResponseUtils.createStatusMessage(response);
        }

        public UploadResponse(string response, string filename, string id, int length, string dateString, OrientationType orientation,
            CameraInfo cameraInfo, string imageDataBase64)
        {
            this.response = response;
            this.filename = filename;
            this.id = id;
            this.length = length;
            this.dateString = dateString;
            this.orientation = orientation;
            this.cameraName = cameraInfo.cameraName;
            this.cameraSerialNumber = cameraInfo.cameraSerialNumber;
            this.cameraSettings = cameraInfo.cameraSettings;
            this.softwareSettings = cameraInfo.softwareSettings;
            this.imageDataBase64 = imageDataBase64;
            this.statusMessage = ResponseUtils.createStatusMessage(response);
        }

        public string response { get; set; }
        public StatusMessage statusMessage { get; set; }
        public string filename { get; set; }
        public string id { get; set; }
        public int length { get; set; }
        public string dateString { get; set; }
        public string cameraName { get; set; }
        public string cameraSerialNumber { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public OrientationType orientation { get; set; }
        public CameraSettings cameraSettings { get; set; }
        public SoftwareSettings softwareSettings { get; set; }
        public string imageDataBase64 { get; set; }
    }
}
