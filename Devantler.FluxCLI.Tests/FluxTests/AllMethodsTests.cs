
using Devantler.KindCLI;

namespace Devantler.FluxCLI.Tests.FluxTests;

/// <summary>
/// Tests for all methods in the <see cref="Flux"/> class.
/// </summary>
public class AllMethodsTests
{
  /// <summary>
  /// Test to verify that flux installs and reconciles kustomizations.
  /// </summary>
  /// <returns></returns>
  [Fact]
  public async Task Flux_InstallsAndReconciles_KustomizationsAsync()
  {
    // Arrange
    string clusterName = "test-cluster";
    string configPath = Path.Combine(AppContext.BaseDirectory, "assets/kind-config.yaml");
    var cancellationToken = new CancellationToken();

    // Act
    await Kind.DeleteClusterAsync(clusterName, CancellationToken.None);
    await Kind.CreateClusterAsync(clusterName, configPath, cancellationToken);
    await Flux.InstallAsync(cancellationToken: cancellationToken);
    await Flux.CreateOCISourceAsync("podinfo", new Uri("oci://ghcr.io/stefanprodan/manifests/podinfo"));
    await Flux.CreateKustomizationAsync("podinfo", "OCIRepository/podinfo", "");
    await Flux.ReconcileAsync(FluxResource.Source, "podinfo", cancellationToken: cancellationToken);
    await Flux.ReconcileAsync(FluxResource.Kustomization, "podinfo", cancellationToken: cancellationToken);

    // Cleanup
    await Kind.DeleteClusterAsync(clusterName, CancellationToken.None);
  }
}