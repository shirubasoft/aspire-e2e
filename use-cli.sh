#!/usr/bin/env bash
set -euo pipefail

TOOL_NAME="shirubasoft.aspire.e2e.cli"
PROJECT_PATH="src/Shirubasoft.Aspire.E2E.Cli/Shirubasoft.Aspire.E2E.Cli.csproj"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

usage() {
    echo "Usage: $0 <local|nuget>"
    echo ""
    echo "  local  - Build and install the CLI globally from the local project"
    echo "  nuget  - Install the CLI globally from NuGet"
    exit 1
}

[[ $# -eq 1 ]] || usage

case "$1" in
    local)
        echo "Packing local CLI..."
        dotnet pack "$SCRIPT_DIR/$PROJECT_PATH" -o "$SCRIPT_DIR/.nupkg" -c Release

        VERSION=$(nbgv get-version -v NuGetPackageVersion)

        echo "Installing local CLI globally (v$VERSION)..."
        dotnet tool update "$TOOL_NAME" \
            --global \
            --allow-downgrade \
            --add-source "$SCRIPT_DIR/.nupkg" \
            --version "$VERSION"
        ;;
    nuget)
        echo "Installing NuGet CLI globally..."
        dotnet tool update "$TOOL_NAME" \
            --global \
            --allow-downgrade
        ;;
    *)
        usage
        ;;
esac

echo "Done."
