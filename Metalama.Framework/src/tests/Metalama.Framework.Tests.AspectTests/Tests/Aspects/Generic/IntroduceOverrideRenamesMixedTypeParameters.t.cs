[TestAspect1]
[TestAspect2]
internal class Target : Base
{
  public override void Foo<X, Y, Z>()
  {
    global::System.Console.WriteLine(typeof(X));
    global::System.Console.WriteLine(typeof(Y));
    global::System.Console.WriteLine(typeof(Z));
    global::System.Console.WriteLine(typeof(X));
  }
}
