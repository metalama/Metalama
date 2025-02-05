[IntroduceAndFilter]
internal class Target
{
  private string? M(string? param)
  {
    if (param == null)
    {
      throw new global::System.ArgumentNullException("param");
    }
    global::System.String? returnValue;
    returnValue = param;
    if (returnValue == null)
    {
      throw new global::System.ArgumentNullException("<return>");
    }
    return returnValue;
  }
  private global::System.String? IntroducedMethod(global::System.String? param)
  {
    if (param == null)
    {
      throw new global::System.ArgumentNullException("param");
    }
    global::System.String? returnValue;
    returnValue = param;
    if (returnValue == null)
    {
      throw new global::System.ArgumentNullException("<return>");
    }
    return returnValue;
  }
}