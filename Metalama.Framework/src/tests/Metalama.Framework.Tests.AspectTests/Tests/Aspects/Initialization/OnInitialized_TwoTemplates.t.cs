[TheAspect]
public class TargetCode
{
  [global::Metalama.Framework.RunTime.Initialization.OnInitializedAttribute]
  public virtual global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_TwoTemplates.TargetCode OnInitialized(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::System.Console.WriteLine("First!");
    global::System.Console.WriteLine("Second!");
    return this;
  }
}