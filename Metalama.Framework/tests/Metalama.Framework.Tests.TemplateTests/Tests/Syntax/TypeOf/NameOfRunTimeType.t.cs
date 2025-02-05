private string Method(MyClass1 a)
{
  var rt = "MyClass1";
  var ct = "MyClass1";
  global::System.Console.WriteLine("rt=" + rt);
  global::System.Console.WriteLine("ct=" + ct);
  global::System.Console.WriteLine("Oops");
  global::System.Console.WriteLine("MyClass1");
  global::System.Console.WriteLine("SingularMethod");
  global::System.Console.WriteLine("OverloadedMethod");
  return this.Method(a);
}