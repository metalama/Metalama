public class Caller
{
  public void Method()
  {
    var x = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new X { Y = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new Y { Z = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new Z()) }) });
  }
}