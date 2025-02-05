internal class Target
{
  private int q;
  [NotZero]
  public int this[int x]
  {
    set
    {
      if (value == 0)
      {
        throw new global::System.ArgumentException();
      }
    }
  }
  [NotZero]
  public int this[int x, int y]
  {
    set
    {
      if (value == 0)
      {
        throw new global::System.ArgumentException();
      }
      q = value + 1;
    }
  }
}