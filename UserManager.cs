using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Grpc.Auth;
using Grpc.Core;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace GoogleAssistantWindows
{
    /// <summary>
    /// Handles the OAuth2 Credentials
    /// </summary>
    public class UserManager
    {
        private const int TokenRefreshTime = 1000 * 60 * 50; // token expires every 60 mins for assistant? The token says 3600s, maybe need to renew the token every minute?

        private UserCredential _credential;
        private Timer _tokenRefreshTimer;

        private GoogleUserData _userData;  
        
        public delegate void UserUpdateDelegate(GoogleUserData userData); 
        public event UserUpdateDelegate OnUserUpdate;

        private static UserManager _instance;

        public static UserManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new UserManager();
                return _instance;
            }
        }

        public ChannelCredentials GetChannelCredential()
        {
            return _credential.ToChannelCredentials();
        }

        public bool IsSignedIn => _credential != null;

        public void SignOut()
        {
            if (_credential != null)
            {
                if (_tokenRefreshTimer != null)
                    _tokenRefreshTimer.Stop();

                _credential.RevokeTokenAsync(CancellationToken.None).Wait();

                foreach (string file in Directory.EnumerateFiles(Utils.GetDataStoreFolder()))
                {
                    if (file.Contains("-user"))
                    {
                        File.Delete(file);
                        return;
                    }
                }
            }

            _credential = null;
        }

        public async Task GetOrRefreshCredential()
        {
            if (_credential == null)
            {
                using (var stream = new FileStream(@"Secrets\client_id.json", FileMode.Open, FileAccess.Read))
                {
                    _credential =
                        await GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets,
                            Const.Scope, Const.User, CancellationToken.None, new FileDataStore(Const.Folder));
                    await _credential.RefreshTokenAsync(CancellationToken.None);
                    await GetGooglePlusUserData(_credential.Token.AccessToken);
                    OnUserUpdate?.Invoke(_userData);
                    StartRefreshTimer();
                }
            }
            else
            {
                await _credential.RefreshTokenAsync(CancellationToken.None);
            }
        }

        private void CreateRefreshTimer()
        {
            _tokenRefreshTimer = new Timer { Interval = TokenRefreshTime };
            _tokenRefreshTimer.Elapsed += (sender2, args) => { GetOrRefreshCredential(); };
        }

        private void StartRefreshTimer()
        {
            if (_tokenRefreshTimer == null)
                CreateRefreshTimer();
            _tokenRefreshTimer.Start();
        }

        private async Task GetGooglePlusUserData(string accessToken)
        {
            try
            {
                HttpClient client = new HttpClient();
                var urlProfile = "https://www.googleapis.com/oauth2/v1/userinfo?access_token=" + accessToken;

                client.CancelPendingRequests();
                HttpResponseMessage output = await client.GetAsync(urlProfile);

                if (output.IsSuccessStatusCode)
                {                  
                    string outputData = await output.Content.ReadAsStringAsync();
                    GoogleUserData newUserData = JsonConvert.DeserializeObject<GoogleUserData>(outputData);

                    if (newUserData != null)
                    {                                                
                        string file = Utils.GetDataStoreFolder() + newUserData.id;
                        string jsonFile = file + ".json";
                        string imageFile = file + ".png";

                        if (File.Exists(jsonFile))
                        {
                            string json = File.ReadAllText(jsonFile);
                            GoogleUserData existingUserData = JsonConvert.DeserializeObject<GoogleUserData>(json);
                            if (newUserData.picture != null &&
                                (!File.Exists(imageFile) ||
                                 existingUserData.picture != null && existingUserData.picture != newUserData.picture))
                                Utils.GetImage(newUserData.picture, imageFile);
                        }
                        else
                            Utils.GetImage(newUserData.picture, imageFile);

                        // overwrites existing file
                        File.WriteAllText(jsonFile, outputData);

                        _userData = newUserData;
                    }
                }
            }
            catch (Exception)
            {
                // return some rubbish for now.
                _userData = new GoogleUserData() { id = "1234567890", name = "Unknown" };
            }
        }           

        public class GoogleUserData
        {
            public string id { get; set; }
            public string name { get; set; }
            public string picture { get; set; }
        }        
    }
}