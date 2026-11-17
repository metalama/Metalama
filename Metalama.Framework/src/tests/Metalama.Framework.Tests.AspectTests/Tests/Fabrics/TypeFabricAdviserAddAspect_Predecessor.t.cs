internal class TargetCode
{
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  private class Fabric : TypeFabric
  {
    public override void AmendType(ITypeAmender amender) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  private void AddedThroughAdviser()
  {
    global::System.Console.WriteLine("Fabric:Fabric TargetCode_Fabric");
    return;
  }
  private void RequiredThroughAdviser()
  {
    global::System.Console.WriteLine("RequiredAspect:Fabric TargetCode_Fabric");
    return;
  }
  private void AddedThroughQuery()
  {
    global::System.Console.WriteLine("Fabric:Fabric TargetCode_Fabric");
    return;
  }
  private void RequiredThroughQuery()
  {
    global::System.Console.WriteLine("RequiredAspect:Fabric TargetCode_Fabric");
    return;
  }
  private void Describe()
  {
    global::System.Console.WriteLine("Fabric:Fabric TargetCode_Fabric");
  }
}