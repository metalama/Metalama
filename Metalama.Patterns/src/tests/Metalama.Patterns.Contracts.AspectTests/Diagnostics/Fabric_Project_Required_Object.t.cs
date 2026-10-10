// Warning LAMA5003 on `foo`: `The [RequiredAttribute] contract on '${p0}' is redundant because a fabric automatically adds the [NotNull] contract to this declaration.`
public class TestClass
{
  // [Required] on object parameter SHOULD trigger LAMA5003 because [Required]
  // and [NotNull] have identical behavior for non-string, non-collection types.
  public void DoSomething([Required] object foo)
  {
    if (foo == null !)
    {
      throw new ArgumentNullException("foo", "The 'foo' parameter is required.");
    }
  }
}
