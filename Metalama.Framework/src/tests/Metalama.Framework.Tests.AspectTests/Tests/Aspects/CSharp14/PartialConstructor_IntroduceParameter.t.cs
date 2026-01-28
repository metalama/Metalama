// Warning CS1066 on `p`: `The default value specified for parameter 'p' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments`
// Warning CS1066 on `p`: `The default value specified for parameter 'p' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments`
// Warning CS1066 on `p`: `The default value specified for parameter 'p' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments`
[TheAspect]
internal partial class ClassWithPartialConstructor
{
  public partial ClassWithPartialConstructor(int x, global::System.Int32 p = 42);
  public partial ClassWithPartialConstructor(int x, global::System.Int32 p = 42)
  {
    Console.WriteLine($"x={x}");
  }
}
[TheAspect]
internal partial class ClassWithTwoPartialConstructors
{
  public partial ClassWithTwoPartialConstructors(global::System.Int32 p = 42);
  public partial ClassWithTwoPartialConstructors(global::System.Int32 p = 42)
  {
    Console.WriteLine("Default");
  }
  public partial ClassWithTwoPartialConstructors(string s, global::System.Int32 p = 42);
  public partial ClassWithTwoPartialConstructors(string s, global::System.Int32 p = 42)
  {
    Console.WriteLine(s);
  }
}
