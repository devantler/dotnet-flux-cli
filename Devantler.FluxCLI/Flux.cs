﻿using System.Globalization;
using System.Runtime.InteropServices;
using CliWrap;
using Devantler.CLIRunner;

namespace Devantler.FluxCLI;

/// <summary>
/// A class to run flux CLI commands.
/// </summary>
public static class Flux
{
  /// <summary>
  /// The flux CLI command.
  /// </summary>
  static Command Command => GetCommand();
  internal static Command GetCommand(PlatformID? platformID = default, Architecture? architecture = default, string? runtimeIdentifier = default)
  {
    platformID ??= Environment.OSVersion.Platform;
    architecture ??= RuntimeInformation.ProcessArchitecture;
    runtimeIdentifier ??= RuntimeInformation.RuntimeIdentifier;

    string binary = (platformID, architecture, runtimeIdentifier) switch
    {
      (PlatformID.Unix, Architecture.X64, "osx-x64") => "flux-osx-x64",
      (PlatformID.Unix, Architecture.Arm64, "osx-arm64") => "flux-osx-arm64",
      (PlatformID.Unix, Architecture.X64, "linux-x64") => "flux-linux-x64",
      (PlatformID.Unix, Architecture.Arm64, "linux-arm64") => "flux-linux-arm64",
      (PlatformID.Win32NT, Architecture.X64, "win-x64") => "flux-win-x64.exe",
      (PlatformID.Win32NT, Architecture.Arm64, "win-arm64") => "flux-win-arm64.exe",
      _ => throw new PlatformNotSupportedException($"Unsupported platform: {Environment.OSVersion.Platform} {RuntimeInformation.ProcessArchitecture}"),
    };
    string binaryPath = Path.Combine(AppContext.BaseDirectory, binary);
    return !File.Exists(binaryPath) ?
      throw new FileNotFoundException($"{binaryPath} not found.") :
      Cli.Wrap(binaryPath);
  }

  /// <summary>
  /// Installs flux in the specified context.
  /// </summary>
  /// <param name="context"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  public static async Task InstallAsync(string? context = default, CancellationToken cancellationToken = default)
  {
    var command = string.IsNullOrEmpty(context) ? Command.WithArguments(["install"]) :
      Command.WithArguments(["install", "--context", context]);
    var (exitCode, message) = await CLI.RunAsync(command, cancellationToken: cancellationToken).ConfigureAwait(false);
    if (exitCode != 0)
    {
      throw new InvalidOperationException($"Failed to install flux: {message}");
    }
  }

  /// <summary>
  /// Creates a OCIRepository source.
  /// </summary>
  /// <param name="name"></param>
  /// <param name="url"></param>
  /// <param name="namespace"></param>
  /// <param name="tag"></param>
  /// <param name="interval"></param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  public static async Task CreateOCISourceAsync(string name, Uri url, string @namespace = "flux-system", string tag = "latest", string interval = "10m")
  {
    ArgumentNullException.ThrowIfNull(url, nameof(url));
    var command = Command.WithArguments(
      ["create", "source", "oci", name, "--url", url.ToString(), "--tag", tag, "--interval", interval, "--namespace", @namespace]
    );
    var (exitCode, message) = await CLI.RunAsync(command).ConfigureAwait(false);
    if (exitCode != 0)
    {
      throw new InvalidOperationException($"Failed to create OCI source: {message}");
    }
  }

  /// <summary>
  /// Creates a Kustomization.
  /// </summary>
  /// <param name="name"></param>
  /// <param name="source"></param>
  /// <param name="path"></param>
  /// <param name="namespace"></param>
  /// <param name="interval"></param>
  /// <param name="dependsOn"></param>
  /// <param name="prune"></param>
  /// <param name="wait"></param>
  /// <returns></returns>
  public static async Task CreateKustomizationAsync(string name, string source, string path, string @namespace = "flux-system", string interval = "5m", string[]? dependsOn = default, bool prune = true, bool wait = true)
  {
    var command = Command.WithArguments(
      ["create", "kustomization", name, "--source", source, "--path", path, "--namespace", @namespace, "--target-namespace", @namespace, "--interval", interval, "--prune", prune.ToString(), "--wait", wait.ToString(), "--depends-on", dependsOn != null ? string.Join(",", dependsOn) : ""]
    );
    var (exitCode, message) = await CLI.RunAsync(command).ConfigureAwait(false);
    if (exitCode != 0)
    {
      throw new InvalidOperationException($"Failed to create Kustomization: {message}");
    }
  }

  /// <summary>
  /// Reconcile sources and resources.
  /// </summary>
  /// <param name="resource"></param>
  /// <param name="name"></param>
  /// <param name="namespace"></param>
  /// <param name="cancellationToken"></param>
  public static async Task ReconcileAsync(FluxResource resource, string name, string @namespace = "flux-system", CancellationToken cancellationToken = default)
  {
    var command = Command.WithArguments(
      ["reconcile", resource.ToString().ToLower(CultureInfo.CurrentCulture), name, "--namespace", @namespace]
    );
    var (exitCode, message) = await CLI.RunAsync(command, cancellationToken: cancellationToken).ConfigureAwait(false);
    if (exitCode != 0)
    {
      throw new InvalidOperationException($"Failed to reconcile {resource}: {message}");
    }
  }
}