internal class Target
{
  public Target([NotNull] ref string m)
  {
    if (m == null)
    {
      throw new global::System.ArgumentNullException("m");
    }
    m = "hello";
    if (m == null)
    {
      throw new global::System.ArgumentNullException("m");
    }
  }
}
