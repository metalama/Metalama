internal class Target
{
  [Aspect]
  private void M(string? s, string? t)
  {
    var x = s;
    var y = t;
    if (x != null)
    {
      if (y != null)
      {
      }
    }
    return;
  }
}
