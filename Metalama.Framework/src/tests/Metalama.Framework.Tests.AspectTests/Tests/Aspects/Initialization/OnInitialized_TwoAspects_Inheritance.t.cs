[FirstAspect]
[SecondAspect]
public class BaseClass
{
  [global::Metalama.Framework.RunTime.Initialization.OnInitializedAttribute]
  public virtual global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_TwoAspects_Inheritance.BaseClass OnInitialized(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::System.Console.WriteLine("First1");
    global::System.Console.WriteLine("First2");
    global::System.Console.WriteLine("Second1");
    global::System.Console.WriteLine("Second2");
    return this;
  }
}
public class DerivedClass : BaseClass
{
  [global::Metalama.Framework.RunTime.Initialization.OnInitializedAttribute]
  public override global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_TwoAspects_Inheritance.BaseClass OnInitialized(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    base.OnInitialized(context.Descend(default));
    global::System.Console.WriteLine("First1");
    global::System.Console.WriteLine("First2");
    global::System.Console.WriteLine("Second1");
    global::System.Console.WriteLine("Second2");
    return this;
  }
}