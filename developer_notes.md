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

1. Update version and push to master ([example](https://github.com/ttu/json-flatfile-datastore/commit/a5d4b1f2099a831ac8c5f37e6db9383ab3c4c20e)). Edit version from csproj with an editor. Also add the version to `CHANGELOG.md` and to the version selector in `docs/index.html`.
2. Smoke test the package before pushing. A push to nuget.org can't be undone — a version number can be unlisted but never reused. `dotnet test` uses project references, so it passes even when the package itself is broken.
```sh
$ ./scripts/smoke-test-package.sh
```
This packs the library, checks the `.nupkg` contents (netstandard2.0 assembly, XML docs, README, LICENSE, Newtonsoft.Json and Microsoft.CSharp dependencies), then builds a throwaway console app that consumes the package from a local feed and exercises the public API. It restores into a temporary `NUGET_PACKAGES` directory so the unpublished version never lands in the global cache, where it would shadow the real package after release.

3. Update Tags
```sh
$ git tag x.x.x
$ git push origin --tags
```
4. Build new release. Check API key from [Nuget](https://www.nuget.org/account/apikeys)
```sh
$ dotnet build --configuration Release
$ dotnet nuget push .\JsonFlatFileDataStore\bin\Release\JsonFlatFileDataStore.x.x.x.nupkg --source https://api.nuget.org/v3/index.json --api-key xxxxx
```


