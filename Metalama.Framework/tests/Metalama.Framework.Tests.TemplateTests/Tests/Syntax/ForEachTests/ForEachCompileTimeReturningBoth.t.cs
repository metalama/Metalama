private void Method()
{
  global::System.Console.WriteLine("1");
  global::System.Console.WriteLine("2");
  global::System.Console.WriteLine("3");
  foreach (var x in new global::System.Int32[]
  {
    1,
    2,
    3
  }
  )
  {
    global::System.Console.WriteLine(x.ToString());
  }
  this.Method();
  return;
}