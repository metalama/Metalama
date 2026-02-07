internal class Target
{
  [Aspect]
  private int M(string? s)
  {
    if (s != null)
    {
      return this.M(s);
    }
    return default(int);
  }
}
