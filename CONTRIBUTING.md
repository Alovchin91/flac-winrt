# FLAC for Windows Runtime

## Contribute to the project

Thank for your interest in FLAC for Windows Runtime project!

If you want to contribute to the project, please review these simple guidelines to make contribution process as easy and nice as possible :)

### Folder structure

**/examples** - Contains a solution that includes all the usage samples of the project. Please see [README.md](./examples/README.md) inside this folder for more info.

**/include** - Contains all _public_ headers of the project that are meant to be included in other C/C++ projects that use FLAC for Windows Runtime.

**/src** - Contains actual project's code including all source code files and _private_ headers.

If you want to contribute by writing some documentation for the project, please put it into the **/doc** folder of the project. Thanks :)

### Source code

Please consider following guidelines for the source code:

* Root namespace is **libFLAC**.
* Create namespaces to denote part of functionality: for example, libFLAC::Decoder, libFLAC::Format, etc.
* Use UpperCamelCase in the names of namespaces (except root namespace), classes and public methods and properties.
* Use lowerCamelCase in the names of parameters and local variables.
* Don't expose public fields. Provide properties with getters and setters instead.
* Try to write internal code using C/C++ native classes. Use Windows Runtime and C++/CX classes only to expose public functionality.
* Don't change original libFLAC's code or the code of any third party library. They're not our projects, so let's leave them to their respective contributors :)
