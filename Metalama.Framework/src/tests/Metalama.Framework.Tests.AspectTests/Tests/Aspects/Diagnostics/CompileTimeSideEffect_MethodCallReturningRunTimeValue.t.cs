internal class Target
{
  [Aspect]
  private int M(string? s)
  {
    var x = s;
    if (x != null)
    {
      return this.M_Source((global::System.String? )x);
    }
    return default;
  }
  private int M_Source(string? s) => 0;
}
