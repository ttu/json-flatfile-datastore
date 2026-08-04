#!/usr/bin/env bash
#
# Packs JsonFlatFileDataStore and consumes the resulting .nupkg from a throwaway
# console app, so packaging defects surface before the irreversible push to nuget.org.
#
# The solution's own tests use project references, so they pass even if the package
# is broken: wrong target framework, missing lib/ assembly, dropped README/LICENSE,
# or a mangled dependency on Newtonsoft.Json. This script catches that class of bug.
#
# Usage: scripts/smoke-test-package.sh
#
# Restores into a temporary NUGET_PACKAGES dir so the unpublished version is never
# written to the global cache, where it would later shadow the real package.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CSPROJ="$REPO_ROOT/JsonFlatFileDataStore/JsonFlatFileDataStore.csproj"

VERSION="$(sed -n 's/.*<PackageVersion>\(.*\)<\/PackageVersion>.*/\1/p' "$CSPROJ")"
if [ -z "$VERSION" ]; then
    echo "FAIL: could not read <PackageVersion> from $CSPROJ" >&2
    exit 1
fi

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

FEED="$WORK/feed"
APP="$WORK/app"
export NUGET_PACKAGES="$WORK/packages"

echo "==> Packing JsonFlatFileDataStore $VERSION"
dotnet pack "$CSPROJ" -c Release -o "$FEED" --nologo -v quiet

NUPKG="$FEED/JsonFlatFileDataStore.$VERSION.nupkg"
[ -f "$NUPKG" ] || { echo "FAIL: expected $NUPKG to exist" >&2; exit 1; }

echo "==> Verifying package contents"
CONTENTS="$(unzip -Z1 "$NUPKG")"
for entry in \
    "lib/netstandard2.0/JsonFlatFileDataStore.dll" \
    "lib/netstandard2.0/JsonFlatFileDataStore.xml" \
    "README.md" \
    "LICENSE"
do
    if grep -Fqx -- "$entry" <<<"$CONTENTS"; then
        echo "    ok  $entry"
    else
        echo "FAIL: $entry missing from package" >&2
        echo "$CONTENTS" >&2
        exit 1
    fi
done

NUSPEC="$(unzip -p "$NUPKG" "JsonFlatFileDataStore.nuspec")"
if grep -q 'targetFramework="\.NETStandard2\.0"' <<<"$NUSPEC"; then
    echo "    ok  netstandard2.0 dependency group"
else
    echo "FAIL: nuspec has no .NETStandard2.0 dependency group" >&2
    echo "$NUSPEC" >&2
    exit 1
fi
for dep in Newtonsoft.Json Microsoft.CSharp; do
    if grep -Fq "id=\"$dep\"" <<<"$NUSPEC"; then
        echo "    ok  depends on $dep"
    else
        echo "FAIL: nuspec is missing a dependency on $dep" >&2
        echo "$NUSPEC" >&2
        exit 1
    fi
done

echo "==> Building consumer app against the packed version"
mkdir -p "$APP"
# No global.json here on purpose: the consumer should build with a stock SDK.
dotnet new console -o "$APP" >/dev/null

cat >"$APP/nuget.config" <<XML
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$FEED" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
XML

cp "$REPO_ROOT/scripts/smoke-test-program.cs" "$APP/Program.cs"

dotnet add "$APP" package JsonFlatFileDataStore -v "$VERSION" >/dev/null

echo "==> Running consumer app"
dotnet run --project "$APP" -c Release -v quiet

echo
echo "PASS: JsonFlatFileDataStore $VERSION is safe to push"
