call buildtools.bat

msbuild BlendShapeBuilderCore.vcxproj /t:Build /p:Configuration=Master /p:Platform=x64 /m /nologo
