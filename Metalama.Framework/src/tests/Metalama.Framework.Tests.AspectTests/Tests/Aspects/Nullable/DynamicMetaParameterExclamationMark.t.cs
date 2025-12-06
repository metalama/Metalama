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
      i.ToString();
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
      s!.ToString();
      return;
    }
    // t1 has no nullability annotation and we cannot detect if ! is redundant
    // without more complex analysis.
    [Aspect]
    private void Generic<T>(T t1)
    {
      t1!.ToString();
      return;
    }
    [Aspect]
    private void NullableGeneric<T>(T? t2)
    {
      t2!.ToString();
      return;
    }
    [Aspect]
    private void NotNullGeneric<T>(T t3)
      where T : notnull
    {
      t3.ToString();
      return;
    }
    [Aspect]
    private void NullableNotNullGeneric<T>(T? t4)
      where T : notnull
    {
      t4!.ToString();
      return;
    }
    [Aspect]
    private void ValueTypeGeneric<T>(T t5)
      where T : struct
    {
      t5.ToString();
      return;
    }
    [Aspect]
    private void NullableValueTypeGeneric<T>(T? t6)
      where T : struct
    {
      t6.ToString();
      return;
    }
    [Aspect]
    private void ReferenceTypeGeneric<T>(T t7)
      where T : class
    {
      t7.ToString();
      return;
    }
    [Aspect]
    private void NullableReferenceTypeGeneric<T>(T? t8)
      where T : class
    {
      t8!.ToString();
      return;
    }
    [Aspect]
    private void ReferenceTypeNullableGeneric<T>(T t9)
      where T : class?
    {
      t9!.ToString();
      return;
    }
    [Aspect]
    private void NullableReferenceTypeNullableGeneric<T>(T? t10)
      where T : class?
    {
      t10!.ToString();
      return;
    }
    [Aspect]
    private void SpecificReferenceTypeGeneric<T>(T t11)
      where T : IComparable
    {
      t11.ToString();
      return;
    }
    [Aspect]
    private void SpecificNullableReferenceTypeGeneric<T>(T? t12)
      where T : IComparable
    {
      t12!.ToString();
      return;
    }
    [Aspect]
    private void SpecificReferenceTypeNullableGeneric<T>(T t13)
      where T : IComparable?
    {
      t13!.ToString();
      return;
    }
    [Aspect]
    private void SpecificNullableReferenceTypeNullableGeneric<T>(T? t14)
      where T : IComparable?
    {
      t14!.ToString();
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
      i.ToString();
      return;
    }
    [Aspect]
    private void ReferenceType(string s)
    {
      s.ToString();
      return;
    }
    [Aspect]
    private void Generic<T>(T t15)
    {
      t15.ToString();
      return;
    }
    [Aspect]
    private void ValueTypeGeneric<T>(T t16)
      where T : struct
    {
      t16.ToString();
      return;
    }
    [Aspect]
    private void NullableValueTypeGeneric<T>(T? t17)
      where T : struct
    {
      t17.ToString();
      return;
    }
    [Aspect]
    private void ReferenceTypeGeneric<T>(T t18)
      where T : class
    {
      t18.ToString();
      return;
    }
    [Aspect]
    private void SpecificReferenceTypeGeneric<T>(T t19)
      where T : IComparable
    {
      t19.ToString();
      return;
    }
  }
}