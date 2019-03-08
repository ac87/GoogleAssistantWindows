using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleAssistantWindows
{
    public class Settings
    {
        private const string SettingsFilename = @"settings.json";
        private const string ClientIdFilename = @"client_id.json";

        public delegate void ClientIdChangedDelegate();
        public event ClientIdChangedDelegate OnClientIdChanged;

        private string clientId;

        public string ClientId {
            get
            {
                return clientId;
            }
            set
            {
                if(!string.Equals(clientId, value))
                {
                    clientId = value;
                    OnClientIdChanged?.Invoke();
                }
            }
        }

        public string DeviceModelId { get; set; }
        public string DeviceId { get; set; }
        public string LanguageCode { get; set; }
        public bool ShowWelcome { get; set; }

        public string ProjectId
        {
            get
            {
                try
                {
                    JObject json = JObject.Parse(ClientId);
                    return (string)json.SelectToken("installed").SelectToken("project_id");
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public void Load()
        {
            if(File.Exists(Utils.GetDataStoreFolder() + ClientIdFilename))
                ClientId = File.ReadAllText(Utils.GetDataStoreFolder() + ClientIdFilename);

            string fileName = Utils.GetDataStoreFolder() + SettingsFilename;

            if (File.Exists(fileName))
            {
                using (TextReader reader = File.OpenText(fileName))
                {
                    JObject json = JObject.Load(new JsonTextReader(reader));
                    DeviceModelId = (string)json.SelectToken("device_model_id");
                    DeviceId = (string)json.SelectToken("device_id");
                    LanguageCode = (string)json.SelectToken("language_code");
                    ShowWelcome = (json.SelectToken("show_welcome") != null) ? (bool)json.SelectToken("show_welcome") : true;
                }
            }
            else
            {
                ShowWelcome = true;
                LanguageCode = "en-US";
            }
        }

        public void Save()
        {
            using (TextWriter textWriter = File.CreateText(Utils.GetDataStoreFolder() + SettingsFilename))
            {
                using (JsonWriter writer = new JsonTextWriter(textWriter))
                {
                    writer.Formatting = Formatting.Indented;

                    writer.WriteStartObject();
                    writer.WritePropertyName("device_model_id");
                    writer.WriteValue(DeviceModelId);
                    writer.WritePropertyName("device_id");
                    writer.WriteValue(DeviceId);
                    writer.WritePropertyName("language_code");
                    writer.WriteValue(LanguageCode);
                    writer.WritePropertyName("show_welcome");
                    writer.WriteValue(ShowWelcome);
                    writer.WriteEndObject();
                }
            }

            File.WriteAllText(Utils.GetDataStoreFolder() + ClientIdFilename, ClientId);
        }
    }
}
