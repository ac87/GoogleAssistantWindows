Remove-Item proto\* -recurse -Force

git clone https://github.com/googleapis/googleapis.git proto

.\Tools\Grpc\tools\windows_x64\protoc.exe --proto_path proto\ --proto_path Tools\Protoc\include\ --csharp_out Generated\assistant --grpc_out Generated\assistant proto\google\assistant\embedded\v1alpha2\embedded_assistant.proto --plugin=protoc-gen-grpc=.\Tools\Grpc\tools/windows_x64/grpc_csharp_plugin.exe

.\Tools\Grpc\tools\windows_x64\protoc.exe --proto_path proto\ --proto_path Tools\Protoc\include\ --csharp_out Generated\googleapis --grpc_out Generated\googleapis proto\google\rpc\status.proto proto\google\api\annotations.proto proto\google\api\http.proto --plugin=protoc-gen-grpc=.\Tools\Grpc\tools/windows_x64/grpc_csharp_plugin.exe

.\Tools\Grpc\tools\windows_x64\protoc.exe --proto_path proto\ --proto_path Tools\Protoc\include\ --csharp_out Generated\googleapis --grpc_out Generated\googleapis proto\google\type\latlng.proto --plugin=protoc-gen-grpc=.\Tools\Grpc\tools/windows_x64/grpc_csharp_plugin.exe
