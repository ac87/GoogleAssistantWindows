Remove-Item -Recurse -Force -ErrorAction Ignore GAssistant\proto\
New-Item -ItemType directory -Path GAssistant\proto\

git clone https://github.com/googleapis/googleapis.git GAssistant\proto

.\Tools\Grpc\tools\windows_x64\protoc.exe --proto_path GAssistant\proto\ --proto_path Tools\Protoc\include\ --csharp_out GAssistant\Generated\assistant --grpc_out GAssistant\Generated\assistant GAssistant\proto\google\assistant\embedded\v1alpha2\embedded_assistant.proto --plugin=protoc-gen-grpc=.\Tools\Grpc\tools/windows_x64/grpc_csharp_plugin.exe

.\Tools\Grpc\tools\windows_x64\protoc.exe --proto_path GAssistant\proto\ --proto_path Tools\Protoc\include\ --csharp_out GAssistant\Generated\googleapis --grpc_out GAssistant\Generated\googleapis GAssistant\proto\google\rpc\status.proto GAssistant\proto\google\api\annotations.proto GAssistant\proto\google\api\http.proto --plugin=protoc-gen-grpc=.\Tools\Grpc\tools/windows_x64/grpc_csharp_plugin.exe

.\Tools\Grpc\tools\windows_x64\protoc.exe --proto_path GAssistant\proto\ --proto_path Tools\Protoc\include\ --csharp_out GAssistant\Generated\googleapis --grpc_out GAssistant\Generated\googleapis GAssistant\proto\google\type\latlng.proto --plugin=protoc-gen-grpc=.\Tools\Grpc\tools/windows_x64/grpc_csharp_plugin.exe

Remove-Item -Recurse -Force -ErrorAction Ignore GAssistant\proto\