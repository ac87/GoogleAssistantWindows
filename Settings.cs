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

        public void Load()
        {
            ClientId = File.ReadAllText(Utils.GetDataStoreFolder() + ClientIdFilename);

            using (TextReader reader = File.OpenText(Utils.GetDataStoreFolder() + SettingsFilename))
            {
                JObject json = JObject.Load(new JsonTextReader(reader));
                DeviceModelId = (string)json.SelectToken("device_model_id");
                DeviceId = (string)json.SelectToken("device_id");
                LanguageCode = (string)json.SelectToken("language_code");
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
                    writer.WriteEndObject();
                }
            }

            File.WriteAllText(Utils.GetDataStoreFolder() + ClientIdFilename, ClientId);
        }
    }
}
