public class Caller
{
  private event Func<TargetCode> _factory = () => global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new TargetCode());
}