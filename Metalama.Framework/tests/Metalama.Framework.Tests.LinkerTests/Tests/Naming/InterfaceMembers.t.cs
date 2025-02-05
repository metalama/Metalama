class Target : ITest
{
  private int _foo;
  int ITest.Foo
  {
    get
    {
      return this._foo;
    }
    set
    {
      this._foo = value;
    }
  }
  int ITest.Bar()
  {
    return this.Metalama_Framework_Tests_LinkerTests_Tests_Naming_InterfaceMembers_ITest_Bar_Source();
  }
  private int Metalama_Framework_Tests_LinkerTests_Tests_Naming_InterfaceMembers_ITest_Bar_Source()
  {
    return 42;
  }
  event EventHandler ITest.Quz
  {
    add
    {
      this.Metalama_Framework_Tests_LinkerTests_Tests_Naming_InterfaceMembers_ITest_Quz_Source += value;
    }
    remove
    {
      this.Metalama_Framework_Tests_LinkerTests_Tests_Naming_InterfaceMembers_ITest_Quz_Source -= value;
    }
  }
  private event EventHandler Metalama_Framework_Tests_LinkerTests_Tests_Naming_InterfaceMembers_ITest_Quz_Source
  {
    add
    {
    }
    remove
    {
    }
  }
}