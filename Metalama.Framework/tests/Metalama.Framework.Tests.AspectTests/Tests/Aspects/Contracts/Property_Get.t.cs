internal class Target
{
  private string? q;
  [NotNull]
  public string P
  {
    get
    {
      global::System.String returnValue;
      returnValue = "p";
      if (returnValue == null)
      {
        throw new global::System.ArgumentNullException();
      }
      return returnValue;
    }
  }
  [NotNull]
  public string Q
  {
    get
    {
      global::System.String returnValue;
      returnValue = q!;
      if (returnValue == null)
      {
        throw new global::System.ArgumentNullException();
      }
      return returnValue;
    }
  }
}