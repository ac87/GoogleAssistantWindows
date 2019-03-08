﻿using System;
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

        
        private Timer _tokenRefreshTimer;

        private GoogleUserData _userData;  
        
        public delegate void UserUpdateDelegate(GoogleUserData userData); 
        public event UserUpdateDelegate OnUserUpdate;

        private Settings settings;

        public UserManager(Settings settings)
        {
            this.settings = settings;
            this.settings.OnClientIdChanged += OnClientIdChanged;
        }

        public UserCredential Credential { get; private set; }

        public ChannelCredentials GetChannelCredential()
        {
            return Credential.ToChannelCredentials();
        }

        public bool IsSignedIn => Credential != null;

        public void SignOut()
        {
            if (Credential != null)
            {
                if (_tokenRefreshTimer != null)
                    _tokenRefreshTimer.Stop();

                Credential.RevokeTokenAsync(CancellationToken.None).Wait();

                foreach (string file in Directory.EnumerateFiles(Utils.GetDataStoreFolder()))
                {
                    if (file.Contains("-user"))
                    {
                        File.Delete(file);
                        return;
                    }
                }
            }

            Credential = null;
        }

        public async Task GetOrRefreshCredential()
        {
            if (Credential == null)
            {
                using (var stream = new FileStream(Utils.GetDataStoreFolder() + @"client_id.json", FileMode.Open, FileAccess.Read))
                {
                    Credential =
                        await GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets,
                            Const.Scope, Const.User, CancellationToken.None, new FileDataStore(Const.Folder));
                    await Credential.RefreshTokenAsync(CancellationToken.None);
                    await GetGooglePlusUserData(Credential.Token.AccessToken);
                    OnUserUpdate?.Invoke(_userData);
                    StartRefreshTimer();
                }
            }
            else
            {
                await Credential.RefreshTokenAsync(CancellationToken.None);
            }
        }

        private void OnClientIdChanged()
        {
            //We Sign-out with the Client Id changes
            SignOut();
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