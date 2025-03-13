[Introduction]
internal class TargetClass
{
  public global::System.Int32 IntroducedProperty
  {
    [global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.CopyAttributes_CrossAssembly.FooAttribute(1)]
    [return: global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.CopyAttributes_CrossAssembly.FooAttribute(2)]
    get
    {
      return 42;
    }
    [global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.CopyAttributes_CrossAssembly.FooAttribute(1)]
    [return: global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.CopyAttributes_CrossAssembly.FooAttribute(2)]
    [param: global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.CopyAttributes_CrossAssembly.FooAttribute(3)]
    set
    {
      var w = 42 + value;
    }
  }
  [global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.CopyAttributes_CrossAssembly.FooAttribute(1)]
  public global::System.Int32 IntroducedProperty_Accessors
  {
    [global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.CopyAttributes_CrossAssembly.FooAttribute(3)]
    [return: global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.CopyAttributes_CrossAssembly.FooAttribute(2)]
    get
    {
      global::System.Console.WriteLine("Get");
      return (global::System.Int32)42;
    }
    [global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.CopyAttributes_CrossAssembly.FooAttribute(5)]
    [return: global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.CopyAttributes_CrossAssembly.FooAttribute(4)]
    [param: global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.CopyAttributes_CrossAssembly.FooAttribute(6)]
    set
    {
      global::System.Console.WriteLine(value);
    }
  }
  [global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.CopyAttributes_CrossAssembly.FooAttribute(1)]
  [field: global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.CopyAttributes_CrossAssembly.FooAttribute(2)]
  public global::System.Int32 IntroducedProperty_Auto {[global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.CopyAttributes_CrossAssembly.FooAttribute(4)]
    [return: global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.CopyAttributes_CrossAssembly.FooAttribute(3)]
    get; [global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.CopyAttributes_CrossAssembly.FooAttribute(6)]
    [return: global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.CopyAttributes_CrossAssembly.FooAttribute(5)]
    set; }
}