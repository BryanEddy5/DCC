using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraControl.Core.Response
{
    class StatusMessage
    {
        public StatusMessage(MessageType messageType)
        {
            this.messageType = messageType;
        }

        public StatusMessage(string shortMessage, string detailMessage, string messageCode, MessageType messageType)
        {
            this.shortMessage = shortMessage;
            this.detailMessage = detailMessage;
            this.messageCode = messageCode;
            this.messageType = messageType;
        }

        public string shortMessage { get; set; } // Show in the UI
        public string detailMessage { get; set; } // Show in a tool tip - what do do
        public string messageCode { get; set; } // e.g., 8D01
        [JsonConverter(typeof(StringEnumConverter))]
        public MessageType messageType { get; set; } // ERROR, WARN, SUCCESS
    }
}
