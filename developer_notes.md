# Developer Notes

### Code coverage

Install `reportgenerator` once if not already installed:
```sh
dotnet tool install -g dotnet-reportgenerator-globaltool
```

Ensure `~/.dotnet/tools` is in your `PATH` (add to `~/.zshrc` if missing):
```sh
export PATH="$HOME/.dotnet/tools:$PATH"
```

```sh
dotnet test JsonFlatFileDataStore.Test/JsonFlatFileDataStore.Test.csproj --collect:"XPlat Code Coverage"
reportgenerator \
  -reports:"JsonFlatFileDataStore.Test/TestResults/**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:Html
open coverage-report/index.html   # macOS (Linux: xdg-open, Windows: start)
```

### Update package version and create a new release

1. Update the version ([example](https://github.com/ttu/json-flatfile-datastore/commit/a5d4b1f2099a831ac8c5f37e6db9383ab3c4c20e)). Edit `Version`, `PackageVersion`, `AssemblyVersion` and `FileVersion` in the csproj with an editor. Also add the version to `CHANGELOG.md` and to the version selector in `docs/index.html`. Don't push yet, smoke test first, so a packaging problem is fixed in the same commit instead of needing a follow-up one.
2. Smoke test the package. A push to nuget.org can't be undone, a version number can be unlisted but never reused. `dotnet test` uses project references, so it passes even when the package itself is broken.
```sh
$ ./scripts/smoke-test-package.sh
```
Packs the library from the current source, checks the `.nupkg` contents, then runs a throwaway console app against it. No prior `dotnet build` needed; the script's header comment explains the details.

3. Commit the version changes and push to master.
4. Update Tags
```sh
$ git tag x.x.x
$ git push origin --tags
```
5. Build new release. Check API key from [Nuget](https://www.nuget.org/account/apikeys)
```sh
$ dotnet build --configuration Release
$ dotnet nuget push .\JsonFlatFileDataStore\bin\Release\JsonFlatFileDataStore.x.x.x.nupkg --source https://api.nuget.org/v3/index.json --api-key xxxxx
```
6. Smoke test the published package. Same checks as step 2, but against the artifact users will actually restore — this catches a bad upload or a package that behaves differently once it comes from the live feed.
```sh
$ ./scripts/smoke-test-package.sh --from-nuget x.x.x
```
nuget.org indexing lags a minute or two behind the push, so a restore failure right afterwards usually just means "not indexed yet" — wait and rerun.


