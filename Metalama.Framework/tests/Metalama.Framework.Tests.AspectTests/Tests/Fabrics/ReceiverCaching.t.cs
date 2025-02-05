// Warning MY001 on `MethodAspect`: `Warning 1: MethodAspect.`
// Warning MY002 on `MethodAspect`: `Warning 2: MethodAspect.`
// Warning MY001 on `PropertyAspect`: `Warning 1: PropertyAspect.`
// Warning MY002 on `PropertyAspect`: `Warning 2: PropertyAspect.`
// Warning MY001 on `TargetCode`: `Warning 1: TargetCode.`
// Warning MY002 on `TargetCode`: `Warning 2: TargetCode.`
// Warning MY001 on `Method2`: `Warning 1: TargetCode.Method2(string).`
// Warning MY002 on `Method2`: `Warning 2: TargetCode.Method2(string).`
// Warning MY001 on `Property1`: `Warning 1: TargetCode.Property1.`
// Warning MY002 on `Property1`: `Warning 2: TargetCode.Property1.`
// Warning MY001 on `AnotherClass`: `Warning 1: AnotherClass.`
// Warning MY002 on `AnotherClass`: `Warning 2: AnotherClass.`
// Warning MY001 on `Method2`: `Warning 1: AnotherClass.Method2(string).`
// Warning MY002 on `Method2`: `Warning 2: AnotherClass.Method2(string).`
// Warning MY001 on `Property1`: `Warning 1: AnotherClass.Property1.`
// Warning MY002 on `Property1`: `Warning 2: AnotherClass.Property1.`
internal class TargetCode
{
  private int Method1(int a) => a;
  private string Method2(string s)
  {
    global::System.Console.WriteLine("overridden instances: 1");
    return s;
  }
  private string Property1
  {
    get
    {
      global::System.Console.WriteLine("overridden instances: 1");
      return "";
    }
  }
}
namespace Sub
{
  internal class AnotherClass
  {
    private int Method1(int a) => a;
    private string Method2(string s)
    {
      global::System.Console.WriteLine("overridden instances: 1");
      return s;
    }
    private string Property1
    {
      get
      {
        global::System.Console.WriteLine("overridden instances: 1");
        return "";
      }
    }
  }
}