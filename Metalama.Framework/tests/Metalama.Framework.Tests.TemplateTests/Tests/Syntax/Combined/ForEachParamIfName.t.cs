private string Method(object a, object bb)
{
  global::System.Console.WriteLine("{0} = {1}", "a", a);
  global::System.Console.WriteLine("{0} = {1}", "bb", bb);
  var result = this.Method(a, bb);
  return (global::System.String)result;
}