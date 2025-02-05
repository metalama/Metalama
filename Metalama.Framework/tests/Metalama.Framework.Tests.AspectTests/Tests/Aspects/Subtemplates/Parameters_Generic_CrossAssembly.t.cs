[Aspect]
public class TargetCode
{
  private global::System.Int32 Add(global::System.Int32 a)
  {
    global::System.Console.WriteLine($"called template T={typeof(global::System.Int32)} a={a} b=1");
    global::System.Console.WriteLine($"called template T={typeof(global::System.Int32)} a={0} b=0");
    global::System.Console.WriteLine($"called template T={typeof(global::System.Int32)} a={a} b=0");
    global::System.Console.WriteLine($"called template T={typeof(global::System.Int32)} a={0} b=1");
    global::System.Console.WriteLine($"called template T={typeof(global::System.Int32)} a={a} b=1");
    global::System.Console.WriteLine("called template T=System.Int32 a=1");
    global::System.Console.WriteLine("called template T=System.Int32 a=0 b=0");
    global::System.Console.WriteLine("called template T=System.Int32 a=1 b=0");
    global::System.Console.WriteLine("called template T=System.Int32 a=0 b=1");
    global::System.Console.WriteLine("called template T=System.Int32 a=1 b=1");
    throw new global::System.Exception();
  }
}