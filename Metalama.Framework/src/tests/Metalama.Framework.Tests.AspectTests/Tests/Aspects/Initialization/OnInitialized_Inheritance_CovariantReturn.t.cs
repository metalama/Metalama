[TheAspect]
public class BaseClass
{
  [global::Metalama.Framework.RunTime.Initialization.OnInitializedAttribute]
  public virtual global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_Inheritance_CovariantReturn.BaseClass OnInitialized(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::System.Console.WriteLine("Initialized BaseClass");
    return this;
  }
}
public class DerivedClass : BaseClass
{
  [global::Metalama.Framework.RunTime.Initialization.OnInitializedAttribute]
  public override global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_Inheritance_CovariantReturn.DerivedClass OnInitialized(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    base.OnInitialized(context.Descend(default));
    global::System.Console.WriteLine("Initialized DerivedClass");
    return this;
  }
}