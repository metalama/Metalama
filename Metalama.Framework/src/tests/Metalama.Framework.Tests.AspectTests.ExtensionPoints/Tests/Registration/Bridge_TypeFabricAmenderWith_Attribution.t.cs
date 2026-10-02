// Warning TEST0001 on `C`: `Registration 'amender' through the adviser: origin 'fabric Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Registration.Bridge_TypeFabricAmenderWith_Attribution.C_Fabric', predecessor Fabric, template provider as expected, stage 0, source stage True.`
// Warning TEST0001 on `M`: `Registration 'amender-with' through the adviser: origin 'fabric Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Registration.Bridge_TypeFabricAmenderWith_Attribution.C_Fabric', predecessor Fabric, template provider as expected, stage 0, source stage True.`
internal class C
{
  private void M()
  {
  }
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  private class Fabric : TypeFabric
  {
    public override void AmendType(ITypeAmender amender) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
}