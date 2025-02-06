using System.Globalization;
using System.Runtime.CompilerServices;
namespace Metalama.Patterns.Memoization.AspectTests.Instance;
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable EqualExpressionComparison
// ReSharper disable ReturnTypeCanBeNotNullable
#pragma warning disable CA2201
internal sealed class TheClass
{
  private int _counter;
  [Memoize]
  public string NonNullableMethod()
  {
    if (_NonNullableMethod == null)
    {
      string value;
      value = this._counter++.ToString(CultureInfo.InvariantCulture);
      global::System.Threading.Interlocked.CompareExchange(ref this._NonNullableMethod, value, null);
    }
    return _NonNullableMethod;
  }
  [Memoize]
  public string? NullableMethod()
  {
    if (_NullableMethod == null)
    {
      var value = new StrongBox<string?>(this.NullableMethod_Source());
      global::System.Threading.Interlocked.CompareExchange(ref this._NullableMethod, value, null);
    }
    return _NullableMethod.Value;
  }
  private string? NullableMethod_Source() => this._counter++.ToString(CultureInfo.InvariantCulture);
  [Memoize]
  public string NonNullableProperty
  {
    get
    {
      if (_NonNullableProperty == null)
      {
        string value;
        value = this._counter++.ToString(CultureInfo.InvariantCulture);
        global::System.Threading.Interlocked.CompareExchange(ref this._NonNullableProperty, value, null);
      }
      return _NonNullableProperty;
    }
  }
  [Memoize]
  public string? NullableProperty
  {
    get
    {
      if (_NullableProperty == null)
      {
        var value = new StrongBox<string?>(NullableProperty_Source);
        global::System.Threading.Interlocked.CompareExchange(ref this._NullableProperty, value, null);
      }
      return _NullableProperty.Value;
    }
  }
  private string? NullableProperty_Source => this._counter++.ToString(CultureInfo.InvariantCulture);
  [Memoize]
  public Guid MethodReturnsStruct()
  {
    if (_MethodReturnsStruct == null)
    {
      var value = new StrongBox<Guid>(this.MethodReturnsStruct_Source());
      global::System.Threading.Interlocked.CompareExchange(ref this._MethodReturnsStruct, value, null);
    }
    return _MethodReturnsStruct.Value;
  }
  private Guid MethodReturnsStruct_Source() => Guid.NewGuid();
  [Memoize]
  public Guid PropertyReturnsStruct
  {
    get
    {
      if (_PropertyReturnsStruct == null)
      {
        var value = new StrongBox<Guid>(PropertyReturnsStruct_Source);
        global::System.Threading.Interlocked.CompareExchange(ref this._PropertyReturnsStruct, value, null);
      }
      return _PropertyReturnsStruct.Value;
    }
  }
  private Guid PropertyReturnsStruct_Source => Guid.NewGuid();
  private StrongBox<Guid> _MethodReturnsStruct;
  private string _NonNullableMethod;
  private string _NonNullableProperty;
  private StrongBox<string?> _NullableMethod;
  private StrongBox<string?> _NullableProperty;
  private StrongBox<Guid> _PropertyReturnsStruct;
}
internal static class Program
{
  public static void Main()
  {
    var o = new TheClass();
    if (o.MethodReturnsStruct() != o.MethodReturnsStruct())
    {
      throw new Exception();
    }
    if (o.PropertyReturnsStruct != o.PropertyReturnsStruct)
    {
      throw new Exception();
    }
    if (o.NonNullableMethod() != o.NonNullableMethod())
    {
      throw new Exception();
    }
    if (o.NullableMethod() != o.NullableMethod())
    {
      throw new Exception();
    }
    if (o.NonNullableProperty != o.NonNullableProperty)
    {
      throw new Exception();
    }
    if (o.NullableProperty != o.NullableProperty)
    {
      throw new Exception();
    }
    Console.WriteLine("Execution OK.");
  }
}