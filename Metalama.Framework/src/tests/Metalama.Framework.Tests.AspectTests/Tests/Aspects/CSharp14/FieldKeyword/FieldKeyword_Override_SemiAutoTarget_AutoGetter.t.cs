[LoggingAspect]
internal class C
{
  private int _value;
  // Auto-implemented getter (no explicit body) combined with a manual setter that uses the 'field' keyword.
  public int Value
  {
    get
    {
      global::System.Console.WriteLine("Getting");
      return _value;
    }
    set
    {
      global::System.Console.WriteLine($"Setting to {value}");
      _value = value;
      this.SetterCallCount++;
    }
  }
  public int SetterCallCount { get; private set; }
}