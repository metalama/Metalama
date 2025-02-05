internal class Target
{
  private int q;
  public int this[[NotZero] int x]
  {
    get
    {
      if (x == 0)
      {
        throw new global::System.ArgumentException();
      }
      Console.WriteLine("Original body");
      return 42;
    }
    set
    {
      if (x == 0)
      {
        throw new global::System.ArgumentException();
      }
      Console.WriteLine("Original body");
    }
  }
  public int this[[NotZero] int x, [NotZero] int y]
  {
    get
    {
      if (x == 0)
      {
        throw new global::System.ArgumentException();
      }
      if (y == 0)
      {
        throw new global::System.ArgumentException();
      }
      Console.WriteLine("Original body");
      return 42;
    }
    set
    {
      if (x == 0)
      {
        throw new global::System.ArgumentException();
      }
      if (y == 0)
      {
        throw new global::System.ArgumentException();
      }
      Console.WriteLine("Original body");
    }
  }
}