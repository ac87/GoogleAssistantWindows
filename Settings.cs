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

        public string ClientId { get; set; }
        public string DeviceModelId { get; set; }
        public string DeviceId { get; set; }

        public void Load()
        {
            ClientId = File.ReadAllText(Utils.GetDataStoreFolder() + @"client_id.json");

            using (TextReader reader = File.OpenText(Utils.GetDataStoreFolder() + SettingsFilename))
            {
                JObject json = JObject.Load(new JsonTextReader(reader));
                DeviceId = (string)json.SelectToken("device_id");
                DeviceModelId = (string)json.SelectToken("device_model_id");
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
                    writer.WritePropertyName("device_id");
                    writer.WriteValue(DeviceId);
                    writer.WritePropertyName("device_model_id");
                    writer.WriteValue(DeviceModelId);
                    writer.WriteEndObject();
                }
            }
        }
    }
}
