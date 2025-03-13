internal class Target
{
  private int q;
  public int this[int x]
  {
    [return: NotZero]
    get
    {
      global::System.Int32 returnValue;
      Console.WriteLine("Original body");
      returnValue = 42;
      if (returnValue == 0)
      {
        throw new global::System.ArgumentException();
      }
      return returnValue;
    }
  }
  public int this[int x, int y]
  {
    [return: NotZero]
    get
    {
      global::System.Int32 returnValue;
      Console.WriteLine("Original body");
      returnValue = 42;
      if (returnValue == 0)
      {
        throw new global::System.ArgumentException();
      }
      return returnValue;
    }
  }
}