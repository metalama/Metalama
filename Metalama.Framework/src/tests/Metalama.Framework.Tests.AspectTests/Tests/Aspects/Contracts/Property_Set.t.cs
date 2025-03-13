internal class Target
{
  private string? q;
  [NotNull]
  public string Q
  {
    set
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException();
      }
      q = value + "-";
    }
  }
}