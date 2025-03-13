internal class TargetCode
{
  private class Nullable
  {
    [Aspect]
    private void ValueType(int i)
    {
      i.ToString();
      return;
    }
    [Aspect]
    private void NullableValueType(int? i)
    {
      i?.ToString();
      return;
    }
    [Aspect]
    private void ReferenceType(string s)
    {
      s.ToString();
      return;
    }
    [Aspect]
    private void NullableReferenceType(string? s)
    {
      s?.ToString();
      return;
    }
    [Aspect]
    private void Generic<T>(T t)
    {
      t?.ToString();
      return;
    }
    [Aspect]
    private void NullableGeneric<T>(T? t)
    {
      t?.ToString();
      return;
    }
    [Aspect]
    private void NotNullGeneric<T>(T t)
      where T : notnull
    {
      t.ToString();
      return;
    }
    [Aspect]
    private void NullableNotNullGeneric<T>(T? t)
      where T : notnull
    {
      t?.ToString();
      return;
    }
    [Aspect]
    private void ValueTypeGeneric<T>(T t)
      where T : struct
    {
      t.ToString();
      return;
    }
    [Aspect]
    private void NullableValueTypeGeneric<T>(T? t)
      where T : struct
    {
      t?.ToString();
      return;
    }
    [Aspect]
    private void ReferenceTypeGeneric<T>(T t)
      where T : class
    {
      t.ToString();
      return;
    }
    [Aspect]
    private void NullableReferenceTypeGeneric<T>(T? t)
      where T : class
    {
      t?.ToString();
      return;
    }
    [Aspect]
    private void ReferenceTypeNullableGeneric<T>(T t)
      where T : class?
    {
      t?.ToString();
      return;
    }
    [Aspect]
    private void NullableReferenceTypeNullableGeneric<T>(T? t)
      where T : class?
    {
      t?.ToString();
      return;
    }
    [Aspect]
    private void SpecificReferenceTypeGeneric<T>(T t)
      where T : IComparable
    {
      t.ToString();
      return;
    }
    [Aspect]
    private void SpecificNullableReferenceTypeGeneric<T>(T? t)
      where T : IComparable
    {
      t?.ToString();
      return;
    }
    [Aspect]
    private void SpecificReferenceTypeNullableGeneric<T>(T t)
      where T : IComparable?
    {
      t?.ToString();
      return;
    }
    [Aspect]
    private void SpecificNullableReferenceTypeNullableGeneric<T>(T? t)
      where T : IComparable?
    {
      t?.ToString();
      return;
    }
  }
#nullable disable
  private class NonNullable
  {
    [Aspect]
    private void ValueType(int i)
    {
      i.ToString();
      return;
    }
    [Aspect]
    private void NullableValueType(int? i)
    {
      i?.ToString();
      return;
    }
    [Aspect]
    private void ReferenceType(string s)
    {
      s?.ToString();
      return;
    }
    [Aspect]
    private void Generic<T>(T t)
    {
      t?.ToString();
      return;
    }
    [Aspect]
    private void ValueTypeGeneric<T>(T t)
      where T : struct
    {
      t.ToString();
      return;
    }
    [Aspect]
    private void NullableValueTypeGeneric<T>(T? t)
      where T : struct
    {
      t?.ToString();
      return;
    }
    [Aspect]
    private void ReferenceTypeGeneric<T>(T t)
      where T : class
    {
      t?.ToString();
      return;
    }
    [Aspect]
    private void SpecificReferenceTypeGeneric<T>(T t)
      where T : IComparable
    {
      t?.ToString();
      return;
    }
  }
}