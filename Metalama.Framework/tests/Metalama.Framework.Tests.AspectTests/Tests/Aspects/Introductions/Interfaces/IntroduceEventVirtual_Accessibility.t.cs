[IntroductionAttribute]
public class TargetType
{
  interface ITest
  {
    internal event global::System.EventHandler TestInternal
    {
      add
      {
        global::System.Console.WriteLine("Default");
      }
      remove
      {
        global::System.Console.WriteLine("Default");
      }
    }
    private protected event global::System.EventHandler TestPrivateProtected
    {
      add
      {
        global::System.Console.WriteLine("Default");
      }
      remove
      {
        global::System.Console.WriteLine("Default");
      }
    }
    protected event global::System.EventHandler TestProtected
    {
      add
      {
        global::System.Console.WriteLine("Default");
      }
      remove
      {
        global::System.Console.WriteLine("Default");
      }
    }
    protected internal event global::System.EventHandler TestProtectedInternal
    {
      add
      {
        global::System.Console.WriteLine("Default");
      }
      remove
      {
        global::System.Console.WriteLine("Default");
      }
    }
    event global::System.EventHandler TestPublic
    {
      add
      {
        global::System.Console.WriteLine("Default");
      }
      remove
      {
        global::System.Console.WriteLine("Default");
      }
    }
  }
}