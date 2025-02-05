[Aspect]
private void Method()
{
  global::System.Console.WriteLine("called template simple aspect a=42");
  global::System.Console.WriteLine("called template invocation aspect a=42 b=1 c=2");
  global::System.Console.WriteLine("called template simple provider a=42");
  global::System.Console.WriteLine("called template invocation provider a=42 b=1 c=2");
  return;
}