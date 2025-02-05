internal class Target
{
  private int q;
  public int this[int x]
  {
    get
    {
      return 42;
    }
    [param: NotZero]
    set
    {
    }
  }
  public int this[int x, int y] { get => q; [param: NotZero]
    set => q = value + 1; }
}