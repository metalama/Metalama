[assembly: MyAspect("Assembly")]
[MyAspect("Type")]
internal class C
{
  [MyAspect("Method")]
  private void M()
  {
    global::System.Console.WriteLine("Aspect order: Method, Type, Assembly");
    return;
  }
}