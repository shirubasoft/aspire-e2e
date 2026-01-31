#!/usr/bin/env bash
# Packs all NuGet packages that get published to nuget.org.
# Used by both the release workflow and the smoke test.
# If you add a new publishable package, add it here.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUT_DIR="${1:-$SCRIPT_DIR/artifacts}"

dotnet pack "$SCRIPT_DIR/src/Shirubasoft.Aspire.E2E.Common/Shirubasoft.Aspire.E2E.Common.csproj" --no-build -c Release -o "$OUT_DIR"
dotnet pack "$SCRIPT_DIR/src/Shirubasoft.Aspire.E2E/Shirubasoft.Aspire.E2E.csproj" --no-build -c Release -o "$OUT_DIR"
dotnet pack "$SCRIPT_DIR/src/Shirubasoft.Aspire.E2E.Hosting/Shirubasoft.Aspire.E2E.Hosting.csproj" --no-build -c Release -o "$OUT_DIR"
# PackAsTool triggers a spurious NU5017 on .NET 10 SDK — the package is created before the error
dotnet pack "$SCRIPT_DIR/src/Shirubasoft.Aspire.E2E.Cli/Shirubasoft.Aspire.E2E.Cli.csproj" -c Release -o "$OUT_DIR" || true
