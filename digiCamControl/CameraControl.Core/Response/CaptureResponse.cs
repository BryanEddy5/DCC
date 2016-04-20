using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraControl.Core.Response
{
    class CaptureResponse
    {
        public CaptureResponse(string response)
        {
            this.response = response;
            this.statusMessage = ResponseUtils.createStatusMessage(response);
        }

        public CaptureResponse(string response, string filename, string id)
        {
            this.response = response;
            this.filename = filename;
            this.id = id;
            this.statusMessage = ResponseUtils.createStatusMessage(response);
        }

        public string response { get; set; }
        public StatusMessage statusMessage { get; set; }
        public string filename { get; set; }
        public string id { get; set; }
    }
}
