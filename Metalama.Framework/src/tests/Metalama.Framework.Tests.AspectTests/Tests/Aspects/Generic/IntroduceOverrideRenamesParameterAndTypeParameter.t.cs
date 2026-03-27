[TestAspect1]
[TestAspect2]
internal class Target : Base
{
  public override U Foo<U>(U item)
  {
    global::System.Console.WriteLine(typeof(U));
    global::System.Console.WriteLine(typeof(U));
    return default(U);
  }
}
