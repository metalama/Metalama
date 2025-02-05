[NotifyPropertyChanged]
internal partial class Person : global::System.ComponentModel.INotifyPropertyChanged
{
  public Person(string firstName, string lastName)
  {
    FirstName = firstName;
    LastName = lastName;
  }
  private string _firstName = default !;
  public string FirstName
  {
    get
    {
      return this._firstName;
    }
    set
    {
      if (value != this._firstName)
      {
        this._firstName = value;
        OnPropertyChanged("FirstName");
      }
      return;
    }
  }
  public string FullName
  {
    get
    {
      return $"{FirstName} {LastName}";
    }
  }
  private string _lastName = default !;
  public string LastName
  {
    get
    {
      return this._lastName;
    }
    set
    {
      if (value != this._lastName)
      {
        this._lastName = value;
        OnPropertyChanged("LastName");
      }
      return;
    }
  }
  protected void OnPropertyChanged(global::System.String name)
  {
    PropertyChanged?.Invoke(this, new global::System.ComponentModel.PropertyChangedEventArgs(name));
  }
  public event global::System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}
[OptionalValueType]
public partial class Account
{
  public string? Name
  {
    get
    {
      return (global::System.String? )((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Request30355.Account.Optional)this.OptionalValues).Name.Value;
    }
    set
    {
      ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Request30355.Account.Optional)this.OptionalValues).Name = new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Request30355.OptionalValue<global::System.String?>(value);
    }
  }
  public Account? Parent
  {
    get
    {
      return (global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Request30355.Account? )((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Request30355.Account.Optional)this.OptionalValues).Parent.Value;
    }
    set
    {
      ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Request30355.Account.Optional)this.OptionalValues).Parent = new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Request30355.OptionalValue<global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Request30355.Account?>(value);
    }
  }
  public partial class Optional
  {
    public global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Request30355.OptionalValue<global::System.String?> Name { get; set; }
    public global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Request30355.OptionalValue<global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Request30355.Account?> Parent { get; set; }
  }
  public global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Request30355.Account.Optional OptionalValues { get; set; } = new Optional();
}