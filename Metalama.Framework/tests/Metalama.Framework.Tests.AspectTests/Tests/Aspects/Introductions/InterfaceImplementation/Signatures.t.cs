[Introduction]
public class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Signatures.IInterface
{
  public T? GenericMethod<T>(T? x)
  {
    return x;
  }
  public T? GenericMethod_DoubleNestedParam<T>(global::System.Collections.Generic.List<global::System.Collections.Generic.List<T>> x)
  {
    if (x.Count > 0)
    {
      if (x[0].Count > 0)
      {
        return (T? )x[0][0];
      }
      else
      {
        return default;
      }
    }
    else
    {
      return default;
    }
  }
  public T? GenericMethod_Multiple<T, U>(T? x, U? y)
  {
    return x;
  }
  public T? GenericMethod_MultipleReverse<T, U>(U? x, T? y)
  {
    return y;
  }
  public T? GenericMethod_NestedParam<T>(global::System.Collections.Generic.List<T> x)
  {
    if (x.Count > 0)
    {
      return (T? )x[0];
    }
    else
    {
      return default;
    }
  }
  public void GenericMethod_Out<T>(out T? x)
  {
    x = default;
  }
  public T? GenericMethod_Ref<T>(ref T? x)
  {
    return x;
  }
  public global::System.Int32 Method(global::System.Int32 x, global::System.String y)
  {
    return x;
  }
  public global::System.Int32 Method_Ref(ref global::System.Int32 x)
  {
    return x;
  }
  public void VoidMethod()
  {
  }
}