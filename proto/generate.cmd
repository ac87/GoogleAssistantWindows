SET tooldir=..\packages\Grpc.Tools.1.3.0\tools\windows_x86\
%tooldir%protoc.exe -I. -I./googleapis --csharp_out ..\Generated\assistant --grpc_out ..\Generated\assistant googleapis/google/assistant/embedded/v1alpha1/embedded_assistant.proto --plugin=protoc-gen-grpc=%tooldir%grpc_csharp_plugin.exe
pause