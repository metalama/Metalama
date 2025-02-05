internal class Target
{
  [Override]
  public Target([NotEmpty][NotNull] string m)
  {
    global::System.Console.WriteLine("Override");
    if (m == null)
    {
      throw new global::System.ArgumentNullException("m");
    }
    if (m.Length == 0)
    {
      throw new global::System.ArgumentNullException("m");
    }
  }
}