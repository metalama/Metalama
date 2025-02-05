using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.NamespaceFabricAddAspectsExclude;
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class Fabric : NamespaceFabric
{
  public override void AmendNamespace(INamespaceAmender amender) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class Aspect : OverrideMethodAspect
{
  public override dynamic? OverrideMethod() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class TargetCode
{
  private int Method1(int a)
  {
    return a;
  }
  private string Method2(string s)
  {
    global::System.Console.WriteLine("overridden");
    return s;
  }
}
[ExcludeAspect(typeof(Aspect))]
internal class ExcludedCode
{
  private int Method1(int a)
  {
    return a;
  }
  private string Method2(string s)
  {
    return s;
  }
}