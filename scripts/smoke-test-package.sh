#!/usr/bin/env bash
#
# Consumes a JsonFlatFileDataStore .nupkg from a throwaway console app, so packaging
# defects surface before -- or immediately after -- the irreversible push to nuget.org.
#
# The solution's own tests use project references, so they pass even if the package
# is broken: wrong target framework, missing lib/ assembly, dropped README/LICENSE,
# or a mangled dependency on Newtonsoft.Json. This script catches that class of bug.
#
# Usage:
#   scripts/smoke-test-package.sh                     # pack locally and test that .nupkg
#   scripts/smoke-test-package.sh --from-nuget 2.4.3  # test the published package
#
# Default (pack) mode is the pre-push gate: it packs the version in the csproj and
# consumes it from a local folder feed. --from-nuget is the post-push check: it skips
# packing and restores the given version straight from nuget.org, verifying the artifact
# that users will actually get. Run it after `dotnet nuget push`; nuget.org indexing can
# lag a minute or two, so a "not found" right after a push may just mean "not yet".
#
# Both modes restore into a temporary NUGET_PACKAGES dir: in pack mode so the unpublished
# version is never written to the global cache where it would later shadow the real
# package, and in --from-nuget mode so the download is real rather than a cache hit.

set -euo pipefail

usage() {
    echo "Usage: scripts/smoke-test-package.sh [--from-nuget <version>]"
}

MODE="pack"
VERSION=""

while [ $# -gt 0 ]; do
    case "$1" in
        --from-nuget)
            MODE="nuget"
            if [ $# -lt 2 ]; then
                echo "FAIL: --from-nuget requires a version, e.g. --from-nuget 2.4.3" >&2
                exit 1
            fi
            VERSION="$2"
            shift 2
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "FAIL: unknown argument: $1" >&2
            usage >&2
            exit 1
            ;;
    esac
done

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CSPROJ="$REPO_ROOT/JsonFlatFileDataStore/JsonFlatFileDataStore.csproj"

if [ "$MODE" = "pack" ]; then
    VERSION="$(sed -n 's/.*<PackageVersion>\(.*\)<\/PackageVersion>.*/\1/p' "$CSPROJ")"
    if [ -z "$VERSION" ]; then
        echo "FAIL: could not read <PackageVersion> from $CSPROJ" >&2
        exit 1
    fi
fi

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

FEED="$WORK/feed"
APP="$WORK/app"
NUGET_ORG="https://api.nuget.org/v3/index.json"
export NUGET_PACKAGES="$WORK/packages"

# Asserts that the .nupkg the consumer app will actually resolve is well formed.
verify_package() {
    local nupkg="$1"

    echo "==> Verifying package contents"
    local contents
    contents="$(unzip -Z1 "$nupkg")"
    for entry in \
        "lib/netstandard2.0/JsonFlatFileDataStore.dll" \
        "lib/netstandard2.0/JsonFlatFileDataStore.xml" \
        "README.md" \
        "LICENSE"
    do
        if grep -Fqx -- "$entry" <<<"$contents"; then
            echo "    ok  $entry"
        else
            echo "FAIL: $entry missing from package" >&2
            echo "$contents" >&2
            exit 1
        fi
    done

    local nuspec
    nuspec="$(unzip -p "$nupkg" "JsonFlatFileDataStore.nuspec")"
    if grep -q 'targetFramework="\.NETStandard2\.0"' <<<"$nuspec"; then
        echo "    ok  netstandard2.0 dependency group"
    else
        echo "FAIL: nuspec has no .NETStandard2.0 dependency group" >&2
        echo "$nuspec" >&2
        exit 1
    fi
    for dep in Newtonsoft.Json Microsoft.CSharp; do
        if grep -Fq "id=\"$dep\"" <<<"$nuspec"; then
            echo "    ok  depends on $dep"
        else
            echo "FAIL: nuspec is missing a dependency on $dep" >&2
            echo "$nuspec" >&2
            exit 1
        fi
    done
}

# Scaffolds the consumer app. nuget.org stays in the source list either way, because the
# transitive Newtonsoft.Json and Microsoft.CSharp dependencies have to come from
# somewhere; source mapping is what keeps JsonFlatFileDataStore itself pinned to the
# feed this run is meant to be testing.
setup_app() {
    mkdir -p "$APP"
    # No global.json here on purpose: the consumer should build with a stock SDK.
    dotnet new console -o "$APP" >/dev/null

    local store_source="$1"
    local sources="    <add key=\"nuget.org\" value=\"$NUGET_ORG\" />"
    local mapping="    <packageSource key=\"nuget.org\">
      <package pattern=\"*\" />
    </packageSource>"

    # Only the local-feed case needs a second source, and therefore a mapping that
    # splits JsonFlatFileDataStore away from everything else.
    if [ "$store_source" = "local" ]; then
        sources="    <add key=\"local\" value=\"$FEED\" />
$sources"
        mapping="    <packageSource key=\"local\">
      <package pattern=\"JsonFlatFileDataStore\" />
    </packageSource>
$mapping"
    fi

    cat >"$APP/nuget.config" <<XML
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
$sources
  </packageSources>
  <packageSourceMapping>
$mapping
  </packageSourceMapping>
</configuration>
XML

    cp "$REPO_ROOT/scripts/smoke-test-program.cs" "$APP/Program.cs"
}

if [ "$MODE" = "pack" ]; then
    echo "==> Packing JsonFlatFileDataStore $VERSION"
    dotnet pack "$CSPROJ" -c Release -o "$FEED" --nologo -v quiet

    NUPKG="$FEED/JsonFlatFileDataStore.$VERSION.nupkg"
    [ -f "$NUPKG" ] || { echo "FAIL: expected $NUPKG to exist" >&2; exit 1; }

    verify_package "$NUPKG"

    echo "==> Building consumer app against the packed version"
    setup_app "local"
    dotnet add "$APP" package JsonFlatFileDataStore -v "$VERSION" >/dev/null
else
    echo "==> Restoring JsonFlatFileDataStore $VERSION from nuget.org"
    setup_app "nuget.org"
    if ! dotnet add "$APP" package JsonFlatFileDataStore -v "$VERSION" >/dev/null; then
        echo "FAIL: could not restore JsonFlatFileDataStore $VERSION from nuget.org" >&2
        echo "      If the push just happened, indexing may still be in progress." >&2
        exit 1
    fi

    # The restore downloaded the real artifact into the temp cache; check that one.
    NUPKG="$NUGET_PACKAGES/jsonflatfiledatastore/$VERSION/jsonflatfiledatastore.$VERSION.nupkg"
    [ -f "$NUPKG" ] || { echo "FAIL: expected $NUPKG to exist after restore" >&2; exit 1; }

    verify_package "$NUPKG"
fi

echo "==> Running consumer app"
dotnet run --project "$APP" -c Release -v quiet

echo
if [ "$MODE" = "pack" ]; then
    echo "PASS: JsonFlatFileDataStore $VERSION is safe to push"
else
    echo "PASS: JsonFlatFileDataStore $VERSION on nuget.org is good"
fi
