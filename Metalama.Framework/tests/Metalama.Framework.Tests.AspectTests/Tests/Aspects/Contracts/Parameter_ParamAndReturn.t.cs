internal class Target
{
  [Filter]
  private string? M(string? param1, int? param2)
  {
    if (param1 == null)
    {
      throw new global::System.ArgumentNullException("param1");
    }
    if (param2 == null)
    {
      throw new global::System.ArgumentNullException("param2");
    }
    global::System.String? returnValue;
    returnValue = param1 + param2.ToString();
    if (returnValue == null)
    {
      throw new global::System.ArgumentNullException("<return>");
    }
    return returnValue;
  }
}