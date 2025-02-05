internal class C
{
  [MyAspect(Property = MyEnum.MyValue)]
  public void M()
  {
    global::System.Console.WriteLine("MyValue");
    return;
  }
}