[OverrideToString]
public partial record TargetRecord
{
  public string Code { get; set; } = "";
  public override global::System.String ToString()
  {
    // <target>
    return (global::System.String)"overridden";
  }
}