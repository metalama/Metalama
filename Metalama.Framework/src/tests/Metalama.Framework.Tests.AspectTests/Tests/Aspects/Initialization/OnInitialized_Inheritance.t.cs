[TheAspect]
public class BaseClass : global::Metalama.Framework.RunTime.Initialization.IInitializable
{
  public virtual void Initialize(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::System.Console.WriteLine("Initialized BaseClass");
  }
}
public class DerivedClass : BaseClass
{
  public override void Initialize(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    base.Initialize(context.Descend(default));
    global::System.Console.WriteLine("Initialized DerivedClass");
  }
}