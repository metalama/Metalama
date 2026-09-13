[Introduction]
public class TargetType
{
  public void ReportNonPositionalClass()
  {
    global::System.Console.WriteLine("() primary=False implicit=True, (TargetType.NonPositionalClass) primary=False implicit=True");
  }
  public void ReportNonPositionalStruct()
  {
    global::System.Console.WriteLine("() primary=False implicit=True");
  }
  public void ReportPositionalClass()
  {
    global::System.Console.WriteLine("(TargetType.PositionalClass) primary=False implicit=True, (int) primary=True implicit=False");
  }
  public void ReportPositionalStruct()
  {
    global::System.Console.WriteLine("() primary=False implicit=True, (int) primary=True implicit=False");
  }
  record NonPositionalClass
  {
  }
  record struct NonPositionalStruct
  {
  }
  record PositionalClass(global::System.Int32 Value)
  {
  }
  record struct PositionalStruct(global::System.Int32 Value)
  {
  }
}