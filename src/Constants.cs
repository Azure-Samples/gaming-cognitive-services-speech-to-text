using System;
using System.Collections.Generic;
using System.Text;

namespace SpeechToText
{
    class Constants
    {
        // Cognitive service
        // If your resource isn't in WEST US, change the endpoints
        public const string AzureKeyURL = "https://westus.api.cognitive.microsoft.com/sts/v1.0/issueToken";
        public const string AzureSpeechToTextURL = "https://westus.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1";
    }
}