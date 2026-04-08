public class Caller
{
  public void Method()
  {
    var t = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new TargetCode(42, context: global::Metalama.Framework.RunTime.Initialization.InitializationContext.WillInitialize));
  }
}