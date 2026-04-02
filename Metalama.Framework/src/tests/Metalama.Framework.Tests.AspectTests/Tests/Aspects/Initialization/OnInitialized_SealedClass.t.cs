[TheAspect]
public sealed class TargetCode
{
  [global::Metalama.Framework.RunTime.Initialization.OnInitializedAttribute]
  public global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_SealedClass.TargetCode OnInitialized(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::System.Console.WriteLine("Initialized!");
    return this;
  }
}