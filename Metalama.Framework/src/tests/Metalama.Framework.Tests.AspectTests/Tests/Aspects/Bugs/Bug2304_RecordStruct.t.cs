[OverrideToString]
public partial record struct TargetRecord
{
  public string? Code { get; set; }
  public override global::System.String ToString()
  {
    // <target>
    return (global::System.String)"overridden";
  }
}
public partial record struct TargetRecord
{
  public int Length => Code?.Length ?? 0;
}