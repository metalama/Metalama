private string Method(string a)
{
  var rt = typeof(global::System.String);
  global::System.Console.WriteLine("rt=" + rt);
  global::System.Console.WriteLine("ct=System.String");
  return this.Method(a);
}