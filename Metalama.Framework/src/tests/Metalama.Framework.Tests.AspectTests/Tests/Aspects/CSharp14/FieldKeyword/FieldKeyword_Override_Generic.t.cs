[LoggingAspect]
internal class C<T>
{
  public T? Value
  {
    get
    {
      return (T? )_value;
    }
    set
    {
      global::System.Console.WriteLine($"Setting Value to {value}");
      _value = value;
    }
  }
  public T? DefaultValue
  {
    get
    {
      return (T? )_defaultValue;
    }
    set
    {
      global::System.Console.WriteLine($"Setting DefaultValue to {value}");
      _defaultValue = value;
    }
  }
  private T? _defaultValue = default;
  private T? _value;
}
