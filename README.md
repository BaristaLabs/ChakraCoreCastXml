ChakraCore CastXML
---

The associated BaristaCore project uses a auto-generated wrapper around ChakraCore named ChakraExternDefinitions.xml
This repository contains the mechanism of generating the ChakraExternDefinitions.xml file directly from ChakraCore source using CastXML

The generated headers xml might be useful for other projects as well.

To Generate ChakraCore decls using Cast XML:

Dependencies:
- Git
- DotNetCore 2.2

```
git clone https://github.com/BaristaLabs/ChakraCoreCastXML
cd ChakraCoreCastXML\BaristaLabs.ChakraCoreCastXml
dotnet run
```

Build CastXML
---

This repo includes a pre-built version of CastXML, it might be desirable to upgrade it from time to time.

To do so, follow the following steps.

Dependencies:
- Git
- VS2017 Community/Professional/Enterprise

``` cmd
choco install cmake --installargs 'ADD_CMAKE_TO_PATH=System'
choco install ninja
"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\VC\Auxiliary\Build\vcvars64.bat"
git clone https://github.com/CastXML/CastXMLSuperbuild.git
mkdir CastXMLSuperbuild-build
cd CastXMLSuperbuild-build
cmake -G Ninja ../CastXMLSuperbuild
cmake --build .
cd castxml-prefix\src\castxml-build
ctest
```

now copy the CastXMLSuperbuild-build/castxml folder to ./lib/castxml

Note: This takes a long time as we're building llvm+clang too. It feels like we're waiting for the heat-death of the universe.

[Reference](https://github.com/CastXML/CastXMLSuperbuild/blob/master/appveyor.yml)