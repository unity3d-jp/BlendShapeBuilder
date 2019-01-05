call buildtools.bat

msbuild VertexTweakerCore.vcxproj /t:Build /p:Configuration=Master /p:Platform=x64 /m /nologo
