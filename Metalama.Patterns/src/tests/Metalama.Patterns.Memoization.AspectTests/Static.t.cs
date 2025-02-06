using System.Globalization;
using System.Runtime.CompilerServices;
namespace Metalama.Patterns.Memoization.AspectTests.Static;
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable EqualExpressionComparison
// ReSharper disable ReturnTypeCanBeNotNullable
#pragma warning disable CA2201
internal static class TheClass
{
  private static int _counter;
  [Memoize]
  public static string NonNullableMethod()
  {
    if (_NonNullableMethod == null)
    {
      string value;
      value = _counter++.ToString(CultureInfo.InvariantCulture);
      global::System.Threading.Interlocked.CompareExchange(ref global::Metalama.Patterns.Memoization.AspectTests.Static.TheClass._NonNullableMethod, value, null);
    }
    return _NonNullableMethod;
  }
  [Memoize]
  public static string? NullableMethod()
  {
    if (_NullableMethod == null)
    {
      var value = new StrongBox<string?>(TheClass.NullableMethod_Source());
      global::System.Threading.Interlocked.CompareExchange(ref global::Metalama.Patterns.Memoization.AspectTests.Static.TheClass._NullableMethod, value, null);
    }
    return _NullableMethod.Value;
  }
  private static string? NullableMethod_Source() => _counter++.ToString(CultureInfo.InvariantCulture);
  [Memoize]
  public static string NonNullableProperty
  {
    get
    {
      if (_NonNullableProperty == null)
      {
        string value;
        value = _counter++.ToString(CultureInfo.InvariantCulture);
        global::System.Threading.Interlocked.CompareExchange(ref global::Metalama.Patterns.Memoization.AspectTests.Static.TheClass._NonNullableProperty, value, null);
      }
      return _NonNullableProperty;
    }
  }
  [Memoize]
  public static string? NullableProperty
  {
    get
    {
      if (_NullableProperty == null)
      {
        var value = new StrongBox<string?>(NullableProperty_Source);
        global::System.Threading.Interlocked.CompareExchange(ref global::Metalama.Patterns.Memoization.AspectTests.Static.TheClass._NullableProperty, value, null);
      }
      return _NullableProperty.Value;
    }
  }
  private static string? NullableProperty_Source => _counter++.ToString(CultureInfo.InvariantCulture);
  [Memoize]
  public static Guid MethodReturnsStruct()
  {
    if (_MethodReturnsStruct == null)
    {
      var value = new StrongBox<Guid>(TheClass.MethodReturnsStruct_Source());
      global::System.Threading.Interlocked.CompareExchange(ref global::Metalama.Patterns.Memoization.AspectTests.Static.TheClass._MethodReturnsStruct, value, null);
    }
    return _MethodReturnsStruct.Value;
  }
  private static Guid MethodReturnsStruct_Source() => Guid.NewGuid();
  [Memoize]
  public static Guid PropertyReturnsStruct
  {
    get
    {
      if (_PropertyReturnsStruct == null)
      {
        var value = new StrongBox<Guid>(PropertyReturnsStruct_Source);
        global::System.Threading.Interlocked.CompareExchange(ref global::Metalama.Patterns.Memoization.AspectTests.Static.TheClass._PropertyReturnsStruct, value, null);
      }
      return _PropertyReturnsStruct.Value;
    }
  }
  private static Guid PropertyReturnsStruct_Source => Guid.NewGuid();
  private static StrongBox<Guid> _MethodReturnsStruct;
  private static string _NonNullableMethod;
  private static string _NonNullableProperty;
  private static StrongBox<string?> _NullableMethod;
  private static StrongBox<string?> _NullableProperty;
  private static StrongBox<Guid> _PropertyReturnsStruct;
}
internal static class Program
{
  public static void Main()
  {
    if (TheClass.MethodReturnsStruct() != TheClass.MethodReturnsStruct())
    {
      throw new Exception();
    }
    if (TheClass.PropertyReturnsStruct != TheClass.PropertyReturnsStruct)
    {
      throw new Exception();
    }
    if (TheClass.NonNullableMethod() != TheClass.NonNullableMethod())
    {
      throw new Exception();
    }
    if (TheClass.NullableMethod() != TheClass.NullableMethod())
    {
      throw new Exception();
    }
    if (TheClass.NonNullableProperty != TheClass.NonNullableProperty)
    {
      throw new Exception();
    }
    if (TheClass.NullableProperty != TheClass.NullableProperty)
    {
      throw new Exception();
    }
    Console.WriteLine("Execution OK.");
  }
}