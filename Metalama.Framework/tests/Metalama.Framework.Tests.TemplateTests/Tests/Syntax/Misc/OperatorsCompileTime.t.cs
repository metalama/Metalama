private object Method(int a, int b)
{
  unchecked
  {
  }
  checked
  {
  }
  var x = 2 switch
  {
    1 => true,
    _ => false
  };
  var t = (x: x, y: true);
  (x: x, y: true) = t;
  bool? z = ((x ^ true) && true) || !x;
  string s = default;
  s ??= "42";
  s = s[0..2];
  global::System.Console.WriteLine(2);
  global::System.Console.WriteLine(t);
  global::System.Console.WriteLine(z.Value);
  global::System.Console.WriteLine(s);
  global::System.Console.WriteLine(sizeof(bool));
  global::System.Console.WriteLine(typeof(global::System.Int32));
  var result = this.Method(a, b);
  return (global::System.Object)result;
}