internal class BaseClass
{
  [InheritedAspect]
  public virtual void ClassMethodWithAspect()
  {
    Console.WriteLine("Hacked!");
  }
  public virtual void ClassMethodWithoutAspect()
  {
  }
}
internal interface IInterface
{
  [InheritedAspect]
  private void InterfaceMethodWithAspect()
  {
    Console.WriteLine("Hacked!");
  }
  private void InterfaceMethodWithoutAspect()
  {
  }
}
internal class DerivedClass : BaseClass, IInterface
{
  public override void ClassMethodWithAspect()
  {
    Console.WriteLine("Hacked!");
    base.ClassMethodWithAspect();
  }
  public override void ClassMethodWithoutAspect()
  {
    base.ClassMethodWithoutAspect();
  }
  public virtual void InterfaceMethodWithAspect()
  {
    Console.WriteLine("Hacked!");
  }
  public virtual void InterfaceMethodWithoutAspect()
  {
  }
}
internal class DerivedTwiceClass : DerivedClass
{
  public override void ClassMethodWithAspect()
  {
    Console.WriteLine("Hacked!");
    base.ClassMethodWithAspect();
  }
  public override void ClassMethodWithoutAspect()
  {
    base.ClassMethodWithoutAspect();
  }
  public override void InterfaceMethodWithAspect()
  {
    Console.WriteLine("Hacked!");
    base.InterfaceMethodWithAspect();
  }
  public override void InterfaceMethodWithoutAspect()
  {
    base.InterfaceMethodWithoutAspect();
  }
}