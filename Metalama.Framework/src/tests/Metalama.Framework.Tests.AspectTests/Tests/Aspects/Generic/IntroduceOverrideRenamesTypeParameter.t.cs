[TestAspect1]
[TestAspect2]
internal class Target : Base
{
  public override void Foo<U>()
  {
    global::System.Console.WriteLine(typeof(U));
    global::System.Console.WriteLine(typeof(U));
  }
}
