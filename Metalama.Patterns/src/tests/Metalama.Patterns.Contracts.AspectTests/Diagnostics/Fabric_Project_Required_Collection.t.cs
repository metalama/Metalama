// Warning LAMA5003 on `items`: `The [RequiredAttribute] contract on '${p0}' is redundant because a fabric automatically adds the [NotNull] contract to this declaration.`
public class TestClass
{
  // [Required] on collection parameter SHOULD trigger LAMA5003 because [Required]
  // and [NotNull] have identical behavior for non-string types.
  public void ProcessItems([Required] ICollection<string> items)
  {
    if (items == null !)
    {
      throw new ArgumentNullException("items", "The 'items' parameter is required.");
    }
  }
}
