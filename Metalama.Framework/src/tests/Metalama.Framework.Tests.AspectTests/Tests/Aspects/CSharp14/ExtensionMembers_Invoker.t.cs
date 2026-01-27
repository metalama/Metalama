internal static class C
{
  extension(TestClass receiver)
  {
    public void InstanceMethod() => Console.WriteLine("Instance method called");
    public static void StaticMethod() => Console.WriteLine("Static method called");
    public string InstanceProperty => "Instance property value";
    public static string StaticProperty => "Static property value";
  }
  [InvokeExtensionMemberAspect]
  public static void TestMethod(TestClass target)
  {
    InstanceMethod(target);
    StaticMethod();
    var value = get_InstanceProperty(target);
    Console.WriteLine(value);
    var value_1 = get_StaticProperty();
    Console.WriteLine(value_1);
  }
}
