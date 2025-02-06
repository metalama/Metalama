// Warning LAMA5002 on `field`: `The [NotNull] contract has been applied to 'NotNull_Eligible_NullableStruct.field', but its type is nullable.`
using System;
namespace Metalama.Patterns.Contracts.AspectTests;
public class NotNull_Eligible_NullableStruct
{
  private DateTime? _field1;
  [NotNull]
  private DateTime? field
  {
    get
    {
      return _field1;
    }
    set
    {
      if (value == null !)
      {
        throw new ArgumentNullException("value", "The 'field' property must not be null.");
      }
      _field1 = value;
    }
  }
}