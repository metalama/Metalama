internal class C
{
  [NotNull]
  public void M(string s)
  {
    if (s == null)
    {
      throw new global::System.ArgumentNullException("s");
    }
  }
}