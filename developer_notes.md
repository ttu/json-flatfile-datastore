# Developer Notes

### Update package version and create a new release

1. Update version and push to master ([example](https://github.com/ttu/json-flatfile-datastore/commit/a5d4b1f2099a831ac8c5f37e6db9383ab3c4c20e)). Edit version from csproj with an editor.
2. Update Tags
```sh
$ git tag x.x.x
$ git push origin --tags
```
3. Build new release. Check API key from [Nuget](https://www.nuget.org/account/apikeys)
```sh
$ dotnet build --configuration Release
$ dotnet nuget push .\JsonFlatFileDataStore\bin\Release\JsonFlatFileDataStore.x.x.x.nupkg --source https://api.nuget.org/v3/index.json --api-key xxxxx
```


