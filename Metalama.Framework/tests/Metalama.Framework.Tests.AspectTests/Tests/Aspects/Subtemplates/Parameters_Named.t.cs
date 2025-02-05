[Aspect]
private void M()
{
  global::System.Console.WriteLine($"called template a={2} b={1} c=4 d=3");
  global::System.Console.WriteLine($"called template a={-1} b={-2} c=-3 d=-4");
  global::System.Console.WriteLine($"called template a={-1} b={2} c=-3 d=4");
  global::System.Console.WriteLine("called template 2 a=2 b=1");
  global::System.Console.WriteLine("called template 2 a=-1 b=-2");
  global::System.Console.WriteLine("called template 2 a=-1 b=2");
  return;
}