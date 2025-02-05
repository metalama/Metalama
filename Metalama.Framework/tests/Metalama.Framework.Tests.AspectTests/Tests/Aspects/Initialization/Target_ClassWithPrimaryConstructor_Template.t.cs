[Aspect]
internal class TargetCode
{
  private string? f;
  private string? f1, f2;
  public string? Property1 { get; }
  public string? Property2 { get; set; }
  public TargetCode()
  {
    this.f = "f";
    this.f1 = "f1";
    this.f2 = "f2";
    this.Property1 = "Property1";
    this.Property2 = "Property2";
  }
}