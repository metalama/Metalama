// Warning CS1066 on `p`: `The default value specified for parameter 'p' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments`
internal partial class C
{
  [TheAspect]
  public partial C(int x, global::System.Int32 p = 42);
  public partial C(int x, global::System.Int32 p = 42)
  {
    global::System.Console.WriteLine("Aspect implementation.");
  }
}
