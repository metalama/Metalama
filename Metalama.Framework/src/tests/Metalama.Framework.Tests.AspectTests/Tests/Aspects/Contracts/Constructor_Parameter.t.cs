internal class Target
{
  public Target([NotNull] string m)
  {
    if (m == null)
    {
      throw new global::System.ArgumentNullException("m");
    }
  }
}