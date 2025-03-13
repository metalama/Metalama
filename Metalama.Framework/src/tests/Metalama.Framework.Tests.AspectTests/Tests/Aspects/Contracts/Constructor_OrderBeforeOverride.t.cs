internal class Target
{
  [Override]
  public Target([NotEmpty][NotNull] string m)
  {
    if (m == null)
    {
      throw new global::System.ArgumentNullException("m");
    }
    if (m.Length == 0)
    {
      throw new global::System.ArgumentNullException("m");
    }
    global::System.Console.WriteLine("Override");
  }
}