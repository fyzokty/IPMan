# Package Validation

Generation-time checks performed:

- All `.csproj`, `.props`, `.xaml` and `.manifest` files that are XML were parsed
  successfully.
- All project references point to files included in the package.
- All projects referenced by `IPMan.sln` exist.
- Solution project GUIDs are unique.
- Package manifest generated.

Not performed:

- `dotnet restore`
- `dotnet build`

Reason: the generation runtime does not have the .NET SDK installed.

The intended first machine validation is Visual Studio 2022 on Windows with the
`.NET desktop development` workload and .NET 8 SDK installed.
