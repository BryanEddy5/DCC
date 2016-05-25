using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraControl.Core.Response
{
    class ResponseUtils
    {
        // var s = jsoncallback + "(" + JsonConvert.SerializeObject(captureResponse) + ");";
        static public string jsonpResponse(string jsonpcallback, string jsonString)
        {
            string s = jsonpcallback + "(" + jsonString + ");";
            return s;
        }

        static public StatusMessage createStatusMessage(string response)
        {
            StatusMessage message = null;
            if (response == "OK")
            {
                message = new StatusMessage(MessageType.SUCCESS);
            }
            else
            {
                int codeIdx = response.IndexOf(": ") + 2;
                string messageCode = null;
                if (response.Length > codeIdx)
                {
                    messageCode = response.Substring(codeIdx);
                    // Generalize this
                    if (messageCode == "8D01")
                    {
                        message = new StatusMessage("The camera did not focus. Please try again.", "Please try taking the picture again", messageCode, MessageType.WARN);
                    }
                    else
                    {
                        message = new StatusMessage(response, "Please try refreshing the browser or turning the camera off and back on", messageCode, MessageType.ERROR);
                    }
                }
                else
                {
                    message = new StatusMessage(response, "Please try refreshing the browser or turning the camera off and back on", null, MessageType.ERROR);
                }
            }
            return message;
        }

    }
}
