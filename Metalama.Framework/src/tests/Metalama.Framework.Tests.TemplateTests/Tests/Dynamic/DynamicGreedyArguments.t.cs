private int Method(PropertyChangedEventArgs a)
{
  this.Method(new global::System.ComponentModel.PropertyChangedEventArgs("a"));
  this.PropertyChanged.Invoke(new global::System.ComponentModel.PropertyChangedEventArgs("a"));
  this.PropertyChanged.Invoke(new global::System.ComponentModel.PropertyChangedEventArgs("a"));
  return default;
}