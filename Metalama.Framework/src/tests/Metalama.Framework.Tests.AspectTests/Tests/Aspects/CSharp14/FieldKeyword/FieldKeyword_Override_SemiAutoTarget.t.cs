[LoggingAspect]
internal class C
{
  private int _value;
  // Semi-automatic property whose manual setter has a side effect.
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