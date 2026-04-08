public class Caller
{
  public void Method()
  {
    // Object initializer — Linker wraps with Initialized()
    var r = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new Range(context: global::Metalama.Framework.RunTime.Initialization.InitializationContext.WillInitialize) { Min = 1, Max = 12 });
    // Derived type — no cast needed with generic Initialized<T>
    var nr = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new NamedRange(context: global::Metalama.Framework.RunTime.Initialization.InitializationContext.WillInitialize) { Name = "test", Min = 1, Max = 10 });
  }
}