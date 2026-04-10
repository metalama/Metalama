[FirstAspect]
[SecondAspect]
public class BaseClass : global::Metalama.Framework.RunTime.Initialization.IInitializable
{
  public virtual void Initialize(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::System.Console.WriteLine("First1");
    global::System.Console.WriteLine("First2");
    global::System.Console.WriteLine("Second1");
    global::System.Console.WriteLine("Second2");
  }
}
public class DerivedClass : BaseClass
{
  public override void Initialize(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    base.Initialize(context);
    global::System.Console.WriteLine("First1");
    global::System.Console.WriteLine("First2");
    global::System.Console.WriteLine("Second1");
    global::System.Console.WriteLine("Second2");
  }
}