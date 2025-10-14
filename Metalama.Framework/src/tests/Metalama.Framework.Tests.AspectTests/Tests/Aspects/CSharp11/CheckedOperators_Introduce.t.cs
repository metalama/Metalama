[TheAspect]
public class C
{
  public static global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.CheckedOperators_Introduce.C operator +(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.CheckedOperators_Introduce.C a, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.CheckedOperators_Introduce.C b)
  {
    global::System.Console.WriteLine("This is BinaryTemplate.");
    return default;
  }
  public static global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.CheckedOperators_Introduce.C operator checked +(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.CheckedOperators_Introduce.C a, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.CheckedOperators_Introduce.C b)
  {
    global::System.Console.WriteLine("This is BinaryTemplate.");
    return default;
  }
  public static explicit operator checked global::System.DateTime(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.CheckedOperators_Introduce.C a)
  {
    global::System.Console.WriteLine("This is UnaryTemplate.");
    return default;
  }
  public static global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.CheckedOperators_Introduce.C operator checked -(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.CheckedOperators_Introduce.C a)
  {
    global::System.Console.WriteLine("This is UnaryTemplate.");
    return default;
  }
  public static explicit operator global::System.DateTime(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.CheckedOperators_Introduce.C a)
  {
    global::System.Console.WriteLine("This is UnaryTemplate.");
    return default;
  }
  public static global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.CheckedOperators_Introduce.C operator -(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.CheckedOperators_Introduce.C a)
  {
    global::System.Console.WriteLine("This is UnaryTemplate.");
    return default;
  }
}