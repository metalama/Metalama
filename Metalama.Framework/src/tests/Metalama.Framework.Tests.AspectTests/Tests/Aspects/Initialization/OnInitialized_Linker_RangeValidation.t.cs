public class Caller
{
  public void Method()
  {
    // Object initializer — Linker wraps with Initialized()
    var r = new Range(context: InitializationContext.WillInitialize)
    {
      Min = 1,
      Max = 12
    }.WithInitialize();
    // Derived type — no cast needed with generic Initialized<T>
    var nr = new NamedRange(context: InitializationContext.WillInitialize)
    {
      Name = "test",
      Min = 1,
      Max = 10
    }.WithInitialize();
  }
}