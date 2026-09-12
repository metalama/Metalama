[Introduction]
public class TargetType : BaseClass
{
  public void ReportTemplate()
  {
    global::System.Console.WriteLine("Ignore returned Ignore, a Class.");
  }
  new enum OtherConflicting
  {
    None
  }
}