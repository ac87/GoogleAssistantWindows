# Welcome to GAssistant
Due to limitations with a free Google Developer Account for the Google Assistant API its not possible for me to use a single developer account for all GAssistant installations. Because of this, every user of GAssistant must create its own Google Developer Account and use this within his client.
 
This guide will explain you how to do the necessary and configure GAssistant. In this guide, I assume that you already have a Google Account. If this is not the case, you can create one using the following link: https://accounts.google.com/SignUp 

## Create/Enable a Google Developer Account for the Google Assistant API
1. In order to create a Google Developer Key, you need to go the Google Actions Console: https://console.actions.google.com/
2. Click on Add/Import Project
3. Create a new project (you can choose the name; I choose GAssistant)
4. Enable the Google Assistant API on the project you created in the Cloud Platform Console: https://console.developers.google.com/apis/library 
   1. Search for "Assistant"
   2. "Enable" of Google Assistant API
5. Configure the OAuth Consent screen for the project you created (most fields are optional and not needed) in the Cloud Platform Console: https://console.developers.google.com/apis/credentials/consent 
 
More information can also be found at: https://developers.google.com/assistant/sdk/guides/service/python/embed/config-dev-project-and-account 
 
## Register a new Device Model
1. Use the Registration UI in the Actions Console and click on Register Model for the project that you created in the previous step
2. Fill in all fields. Ensure that you select "" as Device Type. You will need the Device Model id for the GAssistant Settings
3. Download the credentials JSON file, you will need this for the GAssistant Settings
 
More information can also be found at: https://developers.google.com/assistant/sdk/guides/service/python/embed/register-device 
 
## Register a new Device Instance
You can create a new Device Instance using the Actions Console. But in case you don't explicitly create one, GAssistant will register a device instance for you.
 
## Configure GAssistant 
