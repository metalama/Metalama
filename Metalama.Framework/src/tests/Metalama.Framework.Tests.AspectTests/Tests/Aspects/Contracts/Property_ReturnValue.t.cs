internal class Target
{
  private string? q;
  public string Q
  {
    [return: NotNull]
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