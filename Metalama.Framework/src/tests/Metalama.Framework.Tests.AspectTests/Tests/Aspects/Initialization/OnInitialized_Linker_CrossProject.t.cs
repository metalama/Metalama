public class Caller
{
  public void Method()
  {
    var b = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new BaseClass());
    var d = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new DerivedClass());
  }
}