public class Caller
{
  public void Method()
  {
    // Only required arg supplied — named arg must skip optional 'name'.
    var t1 = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new TargetCode(42, context: global::Metalama.Framework.RunTime.Initialization.InitializationContext.WillInitialize));
    // Both positional args supplied — named arg appended after 'name'.
    var t2 = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new TargetCode(42, "hello", context: global::Metalama.Framework.RunTime.Initialization.InitializationContext.WillInitialize));
  }
}