  [Observable]
  public class TestClass : INotifyPropertyChanged
  {
    [NotObservable]
    public virtual int Foo { get; set; }
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}