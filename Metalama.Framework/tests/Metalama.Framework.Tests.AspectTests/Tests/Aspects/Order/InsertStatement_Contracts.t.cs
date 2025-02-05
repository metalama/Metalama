internal class Target
{
  [return: Test1]
  [return: Test2]
  private int NoOverride([Test1][Test2] ref int p1, [Test1][Test2] ref int p2)
  {
    global::System.Console.WriteLine($"[Test1] on {p1}, ordinal 1");
    global::System.Console.WriteLine($"[Test1] on {p1}, ordinal 2");
    global::System.Console.WriteLine($"[Test2] on {p1}, ordinal 1");
    global::System.Console.WriteLine($"[Test2] on {p1}, ordinal 2");
    global::System.Console.WriteLine($"[Test1] on {p2}, ordinal 1");
    global::System.Console.WriteLine($"[Test1] on {p2}, ordinal 2");
    global::System.Console.WriteLine($"[Test2] on {p2}, ordinal 1");
    global::System.Console.WriteLine($"[Test2] on {p2}, ordinal 2");
    global::System.Int32 returnValue;
    returnValue = 42;
    global::System.Console.WriteLine($"[Test1] on {p1}, ordinal 1");
    global::System.Console.WriteLine($"[Test1] on {p1}, ordinal 2");
    global::System.Console.WriteLine($"[Test2] on {p1}, ordinal 1");
    global::System.Console.WriteLine($"[Test2] on {p1}, ordinal 2");
    global::System.Console.WriteLine($"[Test1] on {p2}, ordinal 1");
    global::System.Console.WriteLine($"[Test1] on {p2}, ordinal 2");
    global::System.Console.WriteLine($"[Test2] on {p2}, ordinal 1");
    global::System.Console.WriteLine($"[Test2] on {p2}, ordinal 2");
    global::System.Console.WriteLine($"[Test1] on {returnValue}, ordinal 1");
    global::System.Console.WriteLine($"[Test1] on {returnValue}, ordinal 2");
    global::System.Console.WriteLine($"[Test2] on {returnValue}, ordinal 1");
    global::System.Console.WriteLine($"[Test2] on {returnValue}, ordinal 2");
    return returnValue;
  }
  [Override]
  [return: Test1]
  [return: Test2]
  private int Override([Test1][Test2] ref int p1, [Test1][Test2] ref int p2)
  {
    global::System.Console.WriteLine($"[Test1] on {p1}, ordinal 1");
    global::System.Console.WriteLine($"[Test1] on {p1}, ordinal 2");
    global::System.Console.WriteLine($"[Test1] on {p2}, ordinal 1");
    global::System.Console.WriteLine($"[Test1] on {p2}, ordinal 2");
    global::System.Int32 returnValue_1;
    global::System.Console.WriteLine($"Override.");
    global::System.Console.WriteLine($"[Test2] on {p1}, ordinal 1");
    global::System.Console.WriteLine($"[Test2] on {p1}, ordinal 2");
    global::System.Console.WriteLine($"[Test2] on {p2}, ordinal 1");
    global::System.Console.WriteLine($"[Test2] on {p2}, ordinal 2");
    global::System.Int32 returnValue;
    returnValue = 42;
    global::System.Console.WriteLine($"[Test2] on {p1}, ordinal 1");
    global::System.Console.WriteLine($"[Test2] on {p1}, ordinal 2");
    global::System.Console.WriteLine($"[Test2] on {p2}, ordinal 1");
    global::System.Console.WriteLine($"[Test2] on {p2}, ordinal 2");
    global::System.Console.WriteLine($"[Test2] on {returnValue}, ordinal 1");
    global::System.Console.WriteLine($"[Test2] on {returnValue}, ordinal 2");
    returnValue_1 = returnValue;
    global::System.Console.WriteLine($"[Test1] on {p1}, ordinal 1");
    global::System.Console.WriteLine($"[Test1] on {p1}, ordinal 2");
    global::System.Console.WriteLine($"[Test1] on {p2}, ordinal 1");
    global::System.Console.WriteLine($"[Test1] on {p2}, ordinal 2");
    global::System.Console.WriteLine($"[Test1] on {returnValue_1}, ordinal 1");
    global::System.Console.WriteLine($"[Test1] on {returnValue_1}, ordinal 2");
    return returnValue_1;
  }
}