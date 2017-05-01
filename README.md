# Google Assistant Windows

First attempt at getting the Google Assistant SDK in a C# WPF application. Created mostly on Windows having never used gRPC or OAuth before. Its not pretty but its marginally nicer than installing Python and using Google's example.

Comprises of 
- Tiny UI to login / Show the logged in user
- Minimise to Tray 
- Global Keyboard shortcut for activation - Ctrl+Alt+G

## Building

1. Clone the repo (optionally with submodules)
2. Restore the Nuget Packages
3. Create your own project on Google Cloud Console with Google Assistant API enabled and OAuth for it as per:
https://developers.google.com/assistant/sdk/prototype/getting-started-other-platforms/config-dev-project-and-account
4. Download the JSON file and move it to `client_id.json` in the `Secrets/` folder 
5. if you don't have Google Assistant or Google Home already also check that link for 'Set activity controls for your account' to ensure your account is setup for Assistant.

This assumes the generated code is up to date, if not..

### Generating the gRPC code

Was done haphazardly, in the `proto/` folder `googleapis` is added as submodule, also in the `proto/` folder is a batch script that runs the C# gRPC generator from the Nuget gRPC tools package. 

I had to copy the `third_party\protobuf\src\google\protobuf` into `googleapis\google` for the build to work, I think this is because under linux this third_party folder would be added to `usr/local/include`

This leaves a few `google.api` files missing, so I had to generate the whole of the googleapis c# gRPC code. I gave up trying to do this in Windows and used a Linux VM instead.


## Resources
http://developers.google.com/assistant/sdk/

http://grpc.io/docs/quickstart/csharp.html

## Credits 

https://www.freesound.org/people/TheGertz/sounds/235911/
https://www.freesound.org/people/cameronmusic/sounds/138417/
https://www.iconfinder.com/icons/1055024/audio_mic_microphone_icon
