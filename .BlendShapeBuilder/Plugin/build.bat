call toolchain.bat

msbuild VertexTweakerCore.vcxproj /t:Build /p:Configuration=Release /p:Platform=x64 /m /nologo
