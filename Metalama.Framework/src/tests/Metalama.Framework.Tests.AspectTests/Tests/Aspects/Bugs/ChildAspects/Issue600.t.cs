internal class TargetClass
{
  private global::System.Object _targetField1 = default !;
  private global::System.Object _targetField
  {
    get
    {
      return this._targetField1;
    }
    set
    {
      this._targetField1 = value;
      return;
    }
  }
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  private class Fabric : TypeFabric
  {
    public override void AmendType(ITypeAmender amender) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
}
