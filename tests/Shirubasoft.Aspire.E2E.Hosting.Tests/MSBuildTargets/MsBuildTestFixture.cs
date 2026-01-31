using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Shirubasoft.Aspire.E2E.Hosting.Tests.MSBuildTargets;

internal sealed class MsBuildTestFixture : IDisposable
{
    private readonly string _tempDir;
    private readonly string _fakeCliDir;

    public string ProjectDir => _tempDir;
    public string IntermediateOutputPath => Path.Combine(_tempDir, "obj");

    public MsBuildTestFixture()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "aspire-e2e-tests", Guid.NewGuid().ToString("N"));
        _fakeCliDir = Path.Combine(_tempDir, "fake-cli");
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(_fakeCliDir);
        Directory.CreateDirectory(IntermediateOutputPath);
    }

    public void WriteFakeCli(Dictionary<string, (string Output, int ExitCode)> responses)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WriteFakeCliWindows(responses);
        }
        else
        {
            WriteFakeCliUnix(responses);
        }
    }

    private void WriteFakeCliUnix(Dictionary<string, (string Output, int ExitCode)> responses)
    {
        var sb = new StringBuilder();
        sb.AppendLine("#!/bin/bash");
        sb.AppendLine("ARGS=\"$*\"");

        bool first = true;
        foreach (var (key, (output, exitCode)) in responses)
        {
            var keyword = first ? "if" : "elif";
            first = false;
            sb.AppendLine($"{keyword} [[ \"$ARGS\" == \"{key}\" ]]; then");
            sb.AppendLine($"  echo \"{output}\"");
            sb.AppendLine($"  exit {exitCode}");
        }

        sb.AppendLine("else");
        sb.AppendLine("  exit 1");
        sb.AppendLine("fi");

        var scriptPath = Path.Combine(_fakeCliDir, "a2a");
        File.WriteAllText(scriptPath, sb.ToString());

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            File.SetUnixFileMode(scriptPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
    }

    private void WriteFakeCliWindows(Dictionary<string, (string Output, int ExitCode)> responses)
    {
        var sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine("set ARGS=%*");

        bool first = true;
        foreach (var (key, (output, exitCode)) in responses)
        {
            var keyword = first ? "if" : ") else if";
            first = false;
            sb.AppendLine($"{keyword} \"%ARGS%\"==\"{key}\" (");
            sb.AppendLine($"  echo {output}");
            sb.AppendLine($"  exit /b {exitCode}");
        }

        sb.AppendLine(") else (");
        sb.AppendLine("  exit /b 1");
        sb.AppendLine(")");

        var scriptPath = Path.Combine(_fakeCliDir, "a2a.cmd");
        File.WriteAllText(scriptPath, sb.ToString());
    }

    public void WriteTestProject(string[] sharedResourceItems)
    {
        // Navigate from bin/Debug/net10.0/ up to repo root (5 levels)
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var propsPath = Path.Combine(repoRoot, "src", "Shirubasoft.Aspire.E2E.Hosting", "build", "Shirubasoft.Aspire.E2E.Hosting.props");
        var targetsPath = Path.Combine(repoRoot, "src", "Shirubasoft.Aspire.E2E.Hosting", "build", "Shirubasoft.Aspire.E2E.Hosting.targets");

        var itemsSb = new StringBuilder();
        foreach (var item in sharedResourceItems)
        {
            itemsSb.AppendLine($"    {item}");
        }

        var csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <IntermediateOutputPath>{IntermediateOutputPath}{Path.DirectorySeparatorChar}</IntermediateOutputPath>
              </PropertyGroup>
              <Import Project="{propsPath}" />
              <Import Project="{targetsPath}" />
              <ItemGroup>
            {itemsSb}
              </ItemGroup>
            </Project>
            """;

        File.WriteAllText(Path.Combine(_tempDir, "TestProject.csproj"), csproj);
    }

    public (string Output, int ExitCode) RunMsBuildTarget(string target)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"msbuild TestProject.csproj /t:{target} /nologo /v:n",
            WorkingDirectory = _tempDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        // Prepend fake CLI dir to PATH
        var pathVar = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Path" : "PATH";
        var currentPath = Environment.GetEnvironmentVariable(pathVar) ?? "";
        psi.Environment[pathVar] = $"{_fakeCliDir}{Path.PathSeparator}{currentPath}";

        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        var combined = $"{stdout}{stderr}".Trim();
        return (combined, process.ExitCode);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Best effort cleanup
        }
    }
}
