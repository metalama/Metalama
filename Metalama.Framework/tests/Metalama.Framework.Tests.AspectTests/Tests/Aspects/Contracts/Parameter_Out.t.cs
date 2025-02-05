internal class Target
{
  private void M([NotNull] out string m)
  {
    m = "";
    if (m == null)
    {
      throw new global::System.ArgumentNullException("m");
    }
  }
}