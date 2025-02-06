internal class Regex : DependencyObject
{
  [DependencyProperty]
  public int Foo
  {
    get
    {
      return (int)GetValue(FooProperty);
    }
    set
    {
      this.SetValue(FooProperty, value);
    }
  }
  private void OnFooChanged()
  {
  }
  private void ValidateFoo(int v)
  {
  }
  [DependencyProperty]
  public string YodaFoo
  {
    get
    {
      return (string)GetValue(TheFooPropertyItIs);
    }
    set
    {
      this.SetValue(TheFooPropertyItIs, value);
    }
  }
  private void MakeFooChanged(string a, string b)
  {
  }
  private void IsFooValid(string s)
  {
  }
  public static readonly DependencyProperty FooProperty;
  public static readonly DependencyProperty TheFooPropertyItIs;
  static Regex()
  {
    FooProperty = DependencyProperty.Register("Foo", typeof(int), typeof(Regex), new PropertyMetadata() { PropertyChangedCallback = (d_1, e) => ((Regex)d_1).OnFooChanged(), CoerceValueCallback = (d, value) =>
    {
      ((Regex)d).ValidateFoo((int)value);
      return value;
    } });
    TheFooPropertyItIs = DependencyProperty.Register("Foo", typeof(string), typeof(Regex), new PropertyMetadata() { PropertyChangedCallback = (d_3, e_1) => ((Regex)d_3).MakeFooChanged((string)e_1.OldValue, (string)e_1.NewValue), CoerceValueCallback = (d_2, value_1) =>
    {
      ((Regex)d_2).IsFooValid((string)value_1);
      return value_1;
    } });
  }
}