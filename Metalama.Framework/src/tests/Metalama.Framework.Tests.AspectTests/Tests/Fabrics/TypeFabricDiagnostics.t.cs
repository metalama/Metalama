// Warning MY001 on `TargetCode`: `Warning on type 'TargetCode'.`
internal class TargetCode
{
  public void Method1()
  {
  }
  public void Method2()
  {
  }
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  private class Fabric : TypeFabric
  {
    private static readonly DiagnosticDefinition<string> _warning = new("MY001", Severity.Warning, "Warning on type '{0}'.");
    public override void AmendType(ITypeAmender amender) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
}