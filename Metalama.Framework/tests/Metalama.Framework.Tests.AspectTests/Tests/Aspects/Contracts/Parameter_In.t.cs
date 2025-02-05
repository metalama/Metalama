internal class Target
{
  private void M([NotNull] string m)
  {
    if (m == null)
    {
      throw new global::System.ArgumentNullException("m");
    }
  }
}