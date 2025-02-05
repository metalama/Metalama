private int Method(int a)
{
  global::System.Console.WriteLine("extra");
  global::System.Console.WriteLine(this.extra != null ? "extra" : "undefined");
  var extra_1 = this.extra != null ? this.extra.Value : 0;
  return (global::System.Int32)(this.Method(a) + extra_1);
}