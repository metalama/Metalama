public class BaseClass
{
  public BaseClass()
  {
    Console.WriteLine("BaseClass()");
  }
  public BaseClass(InitializationContext context)
  {
    Console.WriteLine("BaseClass(InitializationContext)");
  }
}
[InitializerAspect]
public class DerivedClass : BaseClass
{
  public DerivedClass([AspectGenerated] InitializationContext context) : base()
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public DerivedClass() : this(context: default)
  {
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed DerivedClass");
  }
}
[InitializerAspect]
public class ImplicitDerivedClass : BaseClass
{
  public ImplicitDerivedClass([AspectGenerated] InitializationContext context)
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public ImplicitDerivedClass() : this(context: default)
  {
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed ImplicitDerivedClass");
  }
}