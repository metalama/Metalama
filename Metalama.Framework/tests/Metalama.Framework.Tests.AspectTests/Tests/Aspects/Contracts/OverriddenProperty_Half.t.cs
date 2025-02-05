internal class C : B
{
  public override string Default => "C";
  public override string Both
  {
    get
    {
      global::System.String returnValue;
      returnValue = "C";
      if (returnValue == null)
      {
        throw new global::System.ArgumentNullException();
      }
      return returnValue;
    }
  }
  public override string Input => "C";
  public override string Output
  {
    get
    {
      global::System.String returnValue;
      returnValue = "C";
      if (returnValue == null)
      {
        throw new global::System.ArgumentNullException();
      }
      return returnValue;
    }
  }
}