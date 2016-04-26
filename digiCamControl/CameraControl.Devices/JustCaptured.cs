using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraControl.Devices
{
    public class JustCaptured
    {
        public enum CaptureState { GETTING_PREVIEW, GETTING_PHOTO, COMPLETE, CANCELLED };

        public string ImageFilename { get; set; }
        public string PreviewFilename { get; set; }
        public string ImageId { get; set; }
        public CaptureState State { get; set; }
        public string Orientation { get; set; }
    }
}
