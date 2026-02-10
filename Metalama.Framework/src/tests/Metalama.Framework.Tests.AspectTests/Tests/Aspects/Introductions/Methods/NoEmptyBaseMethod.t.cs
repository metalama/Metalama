[Introduction]
internal class TargetClass
{
  public global::System.Int32 IntroduceInt()
  {
    global::System.Console.WriteLine("Before");
    return default(global::System.Int32);
  }
  public global::System.String? IntroduceString()
  {
    global::System.Console.WriteLine("Before");
    return default(global::System.String?);
  }
  public void IntroduceVoid()
  {
    global::System.Console.WriteLine("Before");
    global::System.Console.WriteLine("After");
  }
}
