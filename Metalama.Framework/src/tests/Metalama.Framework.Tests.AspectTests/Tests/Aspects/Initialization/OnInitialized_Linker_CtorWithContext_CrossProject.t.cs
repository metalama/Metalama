public class Caller
{
  public void Method()
  {
    var b = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new BaseClass(1, context: global::Metalama.Framework.RunTime.Initialization.InitializationContext.WillInitialize));
    var d = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new DerivedClass(2, context: global::Metalama.Framework.RunTime.Initialization.InitializationContext.WillInitialize));
  }
}