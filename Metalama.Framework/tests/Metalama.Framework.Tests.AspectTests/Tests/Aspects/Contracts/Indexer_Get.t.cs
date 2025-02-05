internal class Target
{
  private int q;
  [NotZero]
  public int this[int x]
  {
    get
    {
      global::System.Int32 returnValue;
      returnValue = 42;
      if (returnValue == 0)
      {
        throw new global::System.ArgumentException();
      }
      return returnValue;
    }
  }
  [NotZero]
  public int this[int x, int y]
  {
    get
    {
      global::System.Int32 returnValue;
      returnValue = q;
      if (returnValue == 0)
      {
        throw new global::System.ArgumentException();
      }
      return returnValue;
    }
  }
}