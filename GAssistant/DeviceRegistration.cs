using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GoogleAssistantWindows
{
    public class DeviceRegistration
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private const string deviceModelId = "gassistant_windows";
        private const string deviceInstanceId = "my_gassistant_windows";

        public async Task<string> RegisterDeviceModel(string projectId, UserCredential credential)
        {
            string url = string.Format("https://embeddedassistant.googleapis.com/v1alpha2/projects/{0}/deviceModels/", projectId);

            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();
                writer.WritePropertyName("project_id");
                writer.WriteValue(projectId);
                writer.WritePropertyName("device_model_id");
                writer.WriteValue(deviceModelId);
                writer.WritePropertyName("manifest");
                writer.WriteStartObject();
                writer.WritePropertyName("manufacturer");
                writer.WriteValue("GAssistant Open Source");
                writer.WritePropertyName("product_name");
                writer.WriteValue("GAssistant");
                writer.WritePropertyName("device_description");
                writer.WriteValue("Open Source Google Assisant client for Windows.");
                writer.WriteEndObject();
                writer.WritePropertyName("device_type");
                writer.WriteValue("action.devices.types.TV");
                writer.WriteEndObject();
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credential.Token.AccessToken);
                HttpResponseMessage response = await client.PostAsync(url, new StringContent(sb.ToString(), Encoding.UTF8, "application/json"));

                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.Conflict)
                    return deviceModelId;
                else
                {
                    string message = await response.Content.ReadAsStringAsync();
                    logger.Error(message);

                    throw new Exception("Failed to register the device model.");
                }
            }
        }

        public async Task<string> RegisterDeviceInstance(string projectId, UserCredential credential, string modelId)
        {
            string url = string.Format("https://embeddedassistant.googleapis.com/v1alpha2/projects/{0}/devices/", projectId);

            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();
                writer.WritePropertyName("id");
                writer.WriteValue(deviceInstanceId);
                writer.WritePropertyName("model_id");
                writer.WriteValue(modelId);
                writer.WritePropertyName("nickname");
                writer.WriteValue("GAssistant");
                writer.WritePropertyName("client_type");
                writer.WriteValue("SDK_SERVICE");
                writer.WriteEndObject();
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credential.Token.AccessToken);
                HttpResponseMessage response = await client.PostAsync(url, new StringContent(sb.ToString(), Encoding.UTF8, "application/json"));

                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.Conflict)
                    return deviceInstanceId;
                else
                {
                    string message = await response.Content.ReadAsStringAsync();
                    logger.Error(message);

                    throw new Exception("Failed to register the device instance.");
                }
            }
        }
    }
}