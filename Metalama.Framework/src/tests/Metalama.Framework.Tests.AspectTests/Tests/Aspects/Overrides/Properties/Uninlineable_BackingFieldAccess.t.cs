internal class TargetClass
{
  [Override]
  public string? PropertyWithGet
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      _ = this.PropertyWithGet_Source;
      return this.PropertyWithGet_Source;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
      this.PropertyWithGet_Source = value;
      this.PropertyWithGet_Source = value;
    }
  }
  private string? PropertyWithGet_Source { get => field; set; }
  [Override]
  public string? PropertyWithSet
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      _ = this.PropertyWithSet_Source;
      return this.PropertyWithSet_Source;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
      this.PropertyWithSet_Source = value;
      this.PropertyWithSet_Source = value;
    }
  }
  private string? PropertyWithSet_Source { get; set => field = value; }
  [Override]
  public string? PropertyWithGetSet
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      _ = this.PropertyWithGetSet_Source;
      return this.PropertyWithGetSet_Source;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
      this.PropertyWithGetSet_Source = value;
      this.PropertyWithGetSet_Source = value;
    }
  }
  private string? PropertyWithGetSet_Source { get => field; set => field = value; }
}