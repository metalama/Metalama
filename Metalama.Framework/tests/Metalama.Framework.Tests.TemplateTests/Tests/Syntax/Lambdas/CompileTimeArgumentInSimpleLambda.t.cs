private int Method(int a, int b)
{
  global::System.Action<global::System.Object?> action = a_1 => global::System.Console.WriteLine(a_1?.ToString());
  action(1);
  return this.Method(a, b);
}