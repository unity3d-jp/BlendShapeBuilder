call "%VS140COMNTOOLS%..\..\VC\vcvarsall.bat"

msbuild BlendShapeBuilderCore.vcxproj /t:Build /p:Configuration=Master /p:Platform=x64 /m /nologo
