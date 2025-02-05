internal class Target
{
  private void M([NotNull] ref string m)
  {
    if (m == null)
    {
      throw new global::System.ArgumentNullException("m");
    }
    m = "";
    if (m == null)
    {
      throw new global::System.ArgumentNullException("m");
    }
  }
}