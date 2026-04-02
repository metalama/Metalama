[TheAspect]
public class TargetCode
{
  [global::Metalama.Framework.RunTime.Initialization.OnInitializedAttribute]
  public virtual global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_Simple.TargetCode OnInitialized(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::System.Console.WriteLine("Initialized!");
    return this;
  }
}