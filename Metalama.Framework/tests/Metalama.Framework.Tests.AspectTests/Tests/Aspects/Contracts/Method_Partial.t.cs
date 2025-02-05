internal partial class Target
{
  public partial void M([NotNull] string m);
}
internal partial class Target
{
  public partial void M(string m)
  {
    if (m == null)
    {
      throw new global::System.ArgumentNullException("m");
    }
  }
}