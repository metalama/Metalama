internal class Target : ITarget
{
  public string this[int i]
  {
    get
    {
      global::System.String returnValue;
      returnValue = "42";
      if (returnValue == null)
      {
        throw new global::System.ArgumentNullException();
      }
      return returnValue;
    }
    set
    {
    }
  }
}