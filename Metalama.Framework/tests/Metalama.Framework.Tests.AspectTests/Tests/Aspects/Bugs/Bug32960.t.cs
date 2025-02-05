public class PartProvider
{
  [ReportAndSwallowExceptions]
  public string GetPart(string name)
  {
    try
    {
      throw new Exception("This method has a bug.");
    }
    catch (global::System.Exception e)
    {
      global::System.Console.WriteLine(e);
      return default;
    }
  }
}