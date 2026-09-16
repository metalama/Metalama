[Introduction]
public class TargetType : BaseClass
{
  public void ReportTemplate()
  {
    global::System.Console.WriteLine("Ignore reported the ignored outcome: True. The existing type is a Class.");
  }
  new enum OtherConflicting
  {
    None
  }
}