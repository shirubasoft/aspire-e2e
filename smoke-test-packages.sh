#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ARTIFACTS_DIR="$SCRIPT_DIR/artifacts"
APPHOST_DIR="$SCRIPT_DIR/sample/sample.AppHost"
SOURCE_NAME="local-smoke"
PACKAGES_PROPS="$SCRIPT_DIR/Directory.Packages.props"

cleanup() {
    git checkout -- "$PACKAGES_PROPS" 2>/dev/null || true
    rm -f "$APPHOST_DIR/nuget.config"
}
trap cleanup EXIT

echo "==> Building solution in Release..."
dotnet restore "$SCRIPT_DIR"
dotnet build "$SCRIPT_DIR" --no-restore -c Release

echo "==> Packing publishable packages into $ARTIFACTS_DIR..."
rm -rf "$ARTIFACTS_DIR"
"$SCRIPT_DIR/pack-nugets.sh" "$ARTIFACTS_DIR"

VERSION=$(nbgv get-version -v NuGetPackageVersion)
echo "==> Package version: $VERSION"
echo "==> Packages produced:"
ls "$ARTIFACTS_DIR"/*.nupkg

echo "==> Patching Directory.Packages.props to version $VERSION..."
sed -i "s|<PackageVersion Include=\"Shirubasoft.Aspire.E2E\" Version=\"[^\"]*\"|<PackageVersion Include=\"Shirubasoft.Aspire.E2E\" Version=\"$VERSION\"|" "$PACKAGES_PROPS"
sed -i "s|<PackageVersion Include=\"Shirubasoft.Aspire.E2E.Hosting\" Version=\"[^\"]*\"|<PackageVersion Include=\"Shirubasoft.Aspire.E2E.Hosting\" Version=\"$VERSION\"|" "$PACKAGES_PROPS"

echo "==> Creating temporary nuget.config with local feed..."
cat > "$APPHOST_DIR/nuget.config" <<NUGETEOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="local-smoke" value="$ARTIFACTS_DIR" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
    <packageSource key="local-smoke">
      <package pattern="Shirubasoft.*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
NUGETEOF

echo "==> Clearing NuGet cache for Shirubasoft packages..."
NUGET_PACKAGES_DIR="$(dotnet nuget locals global-packages -l | sed 's/.*: //')"
rm -rf "$NUGET_PACKAGES_DIR"/shirubasoft.*

echo "==> Restoring sample AppHost from local feed..."
dotnet restore "$APPHOST_DIR" --configfile "$APPHOST_DIR/nuget.config" -p:UseProjectReferences=false

echo "==> Smoke test passed — all packages resolve correctly."
