internal class Target
{
  public Target([NotNull] out string m)
  {
    m = "";
    if (m == null)
    {
      throw new global::System.ArgumentNullException("m");
    }
  }
}
