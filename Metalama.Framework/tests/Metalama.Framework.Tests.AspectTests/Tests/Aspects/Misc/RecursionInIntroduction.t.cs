[IntroductionAspect]
internal class MyClass
{
  public global::System.Int32 Ackermann1(global::System.Int32 m, global::System.Int32 n)
  {
    if (m == 0)
    {
      return (global::System.Int32)(n + 1);
    }
    else if (n == 0)
    {
      return (global::System.Int32)Ackermann1(m - 1, 1);
    }
    else
    {
      return (global::System.Int32)Ackermann1(m - 1, Ackermann1(m, n - 1));
    }
  }
  public global::System.Int32 Ackermann2(global::System.Int32 m, global::System.Int32 n)
  {
    if (m == 0)
    {
      return (global::System.Int32)(n + 1);
    }
    else if (n == 0)
    {
      return (global::System.Int32)this.Ackermann2(m - 1, 1);
    }
    else
    {
      return (global::System.Int32)this.Ackermann2(m - 1, this.Ackermann2(m, n - 1));
    }
  }
  public global::System.Int32 Ackermann3(global::System.Int32 m, global::System.Int32 n)
  {
    if (m == 0)
    {
      return (global::System.Int32)(n + 1);
    }
    else if (n == 0)
    {
      return this.Ackermann3(m - 1, 1);
    }
    else
    {
      return this.Ackermann3(m - 1, this.Ackermann3(m, n - 1));
    }
  }
}