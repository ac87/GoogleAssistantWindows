New-Item Tools -type directory -Force

Invoke-WebRequest -Uri https://www.nuget.org/api/v2/package/Grpc.Tools/ -OutFile grpc.zip

Remove-Item Tools\Grpc\* -recurse
Expand-Archive grpc.zip -DestinationPath Tools\Grpc
Remove-Item grpc.zip

Invoke-WebRequest -Uri https://github.com/google/protobuf/releases/download/v3.5.0/protoc-3.5.0-win32.zip -OutFile protoc.zip

Remove-Item Tools\Protoc\* -recurse
Expand-Archive protoc.zip -DestinationPath Tools\Protoc
Remove-Item protoc.zip