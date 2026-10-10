public class ExplicitRegistrationFieldOnly : DependencyObject
{
  // The explicit name must be used even when no callback method is specified.
  [DependencyProperty(RegistrationField = "TheFooProperty")]
  public int Foo
  {
    get
    {
      return (int)GetValue(TheFooProperty);
    }
    set
    {
      this.SetValue(TheFooProperty, value);
    }
  }
  // Found by the default callback name, which still applies.
  private void OnFooChanged()
  {
  }
  // Conflicts with the name determined by the default naming convention.
  private void FooProperty()
  {
  }
  public static readonly DependencyProperty TheFooProperty;
  static ExplicitRegistrationFieldOnly()
  {
    TheFooProperty = DependencyProperty.Register("Foo", typeof(int), typeof(ExplicitRegistrationFieldOnly), new PropertyMetadata((d, e) => ((ExplicitRegistrationFieldOnly)d).OnFooChanged()));
  }
}