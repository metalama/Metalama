internal partial class Target
{
  public void M1([NotNull] string m)
  {
    if (m == null)
    {
      throw new global::System.ArgumentNullException("m");
    }
    throw new Exception();
  }
  public int M2([NotNull] string m)
  {
    if (m == null)
    {
      throw new global::System.ArgumentNullException("m");
    }
    throw new Exception();
  }
}