// Warning TEST0003 on `Greet`: `The redirection of 'new Greeter().Greet' was refused with ArgumentException: The method group 'new Greeter().Greet' has a receiver, so it cannot be redirected with the receiver mode Drop.`
[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    Action action = new Greeter().Greet;
    action();
  }
}