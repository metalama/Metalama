using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
namespace Metalama.Framework.Tests.AspectTests.Tests.Fabrics.DeclarativeAdviceWithTemplateInitialization;
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class BuildInfo
{
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  private class Fabric : TypeFabric
  {
    [Introduce]
    public string? TargetFramework { get; }
    [Introduce]
    public string? Configuration { get; }
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  public global::System.String? Configuration { get; } = "Debug";
  public global::System.String? TargetFramework { get; } = "net6.0";
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052