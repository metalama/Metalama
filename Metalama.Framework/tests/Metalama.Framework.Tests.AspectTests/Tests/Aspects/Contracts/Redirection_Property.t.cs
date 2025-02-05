[RedirectingAspect]
internal class Target
{
  private string? q;
  [NotNull]
  public string P => "p";
  [NotNull]
  public string Q
  {
    get
    {
      return q!;
    }
  }
  public void Foo(global::System.String p, global::System.String q)
  {
    if (p == null)
    {
      throw new global::System.ArgumentNullException();
    }
    if (q == null)
    {
      throw new global::System.ArgumentNullException();
    }
  }
}