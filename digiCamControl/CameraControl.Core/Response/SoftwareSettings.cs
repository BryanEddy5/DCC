using CameraControl.Core.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraControl.Core.Response
{
    class SoftwareSettings
    {
        public SoftwareSettings(CameraProperty property)
        {
            this.LiveviewSettings = property.LiveviewSettings;
        }

        public LiveviewSettings LiveviewSettings { get; set; }
    }
}
