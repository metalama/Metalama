internal class TargetCode
{
  private class Nullable
  {
    [Aspect]
    private void ValueType(int arg)
    {
      arg.ToString();
    }
    [Aspect]
    private void NullableValueType(int? arg)
    {
      arg?.ToString();
    }
    [Aspect]
    private void ReferenceType(string arg)
    {
      arg.ToString();
    }
    [Aspect]
    private void NullableReferenceType(string? arg)
    {
      arg?.ToString();
    }
    [Aspect]
    private void Generic<T>(T arg)
    {
      arg?.ToString();
    }
    [Aspect]
    private void NullableGeneric<T>(T? arg)
    {
      arg?.ToString();
    }
    [Aspect]
    private void NotNullGeneric<T>(T arg)
      where T : notnull
    {
      arg.ToString();
    }
    [Aspect]
    private void NullableNotNullGeneric<T>(T? arg)
      where T : notnull
    {
      arg?.ToString();
    }
    [Aspect]
    private void ValueTypeGeneric<T>(T arg)
      where T : struct
    {
      arg.ToString();
    }
    [Aspect]
    private void NullableValueTypeGeneric<T>(T? arg)
      where T : struct
    {
      arg?.ToString();
    }
    [Aspect]
    private void ReferenceTypeGeneric<T>(T arg)
      where T : class
    {
      arg.ToString();
    }
    [Aspect]
    private void NullableReferenceTypeGeneric<T>(T? arg)
      where T : class
    {
      arg?.ToString();
    }
    [Aspect]
    private void ReferenceTypeNullableGeneric<T>(T arg)
      where T : class?
    {
      arg?.ToString();
    }
    [Aspect]
    private void NullableReferenceTypeNullableGeneric<T>(T? arg)
      where T : class?
    {
      arg?.ToString();
    }
    [Aspect]
    private void SpecificReferenceTypeGeneric<T>(T arg)
      where T : IComparable
    {
      arg.ToString();
    }
    [Aspect]
    private void SpecificNullableReferenceTypeGeneric<T>(T? arg)
      where T : IComparable
    {
      arg?.ToString();
    }
    [Aspect]
    private void SpecificReferenceTypeNullableGeneric<T>(T arg)
      where T : IComparable?
    {
      arg?.ToString();
    }
    [Aspect]
    private void SpecificNullableReferenceTypeNullableGeneric<T>(T? arg)
      where T : IComparable?
    {
      arg?.ToString();
    }
  }
#nullable disable
  private class NonNullable
  {
    [Aspect]
    private void ValueType(int arg)
    {
      arg.ToString();
    }
    [Aspect]
    private void NullableValueType(int? arg)
    {
      arg?.ToString();
    }
    [Aspect]
    private void ReferenceType(string arg)
    {
      arg?.ToString();
    }
    [Aspect]
    private void Generic<T>(T arg)
    {
      arg?.ToString();
    }
    [Aspect]
    private void ValueTypeGeneric<T>(T arg)
      where T : struct
    {
      arg.ToString();
    }
    [Aspect]
    private void NullableValueTypeGeneric<T>(T? arg)
      where T : struct
    {
      arg?.ToString();
    }
    [Aspect]
    private void ReferenceTypeGeneric<T>(T arg)
      where T : class
    {
      arg?.ToString();
    }
    [Aspect]
    private void SpecificReferenceTypeGeneric<T>(T arg)
      where T : IComparable
    {
      arg?.ToString();
    }
  }
}