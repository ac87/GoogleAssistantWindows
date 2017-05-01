namespace GoogleAssistantWindows
{
    public class Const
    {
        public const string Folder = "GoogleAssistantWindows";
        public const string AssistantEndpoint = "embeddedassistant.googleapis.com";
        public const string AssistantScope = "https://www.googleapis.com/auth/assistant-sdk-prototype";
        public static readonly string[] Scope = { "openid", AssistantScope };
        public const string User = "user";

        public const int SampleRateHz = 16000;
        public const int MaxRecordMillis = 30 * 1000;
    }
}
