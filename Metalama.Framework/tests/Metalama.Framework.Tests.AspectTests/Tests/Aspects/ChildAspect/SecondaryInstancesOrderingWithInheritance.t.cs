[assembly: MyAspect("Assembly")]
[MyAspect("BaseClass")]
internal class BaseClass
{
  [MyAspect("BaseMethod.M")]
  public virtual void M()
  {
    global::System.Console.WriteLine("Aspect order: BaseMethod.M, BaseClass(ChildAspect,1), Assembly(ChildAspect,1)");
    return;
  }
}
[MyAspect("DerivedClass")]
internal class DerivedClass : BaseClass
{
  [MyAspect("DerivedClass.M")]
  public override void M()
  {
    global::System.Console.WriteLine("Aspect order: DerivedClass.M, DerivedClass(ChildAspect,1), BaseMethod.M(Inherited,1), Assembly(ChildAspect,1), BaseClass(Inherited,2), Assembly(Inherited,2)");
    return;
  }
}