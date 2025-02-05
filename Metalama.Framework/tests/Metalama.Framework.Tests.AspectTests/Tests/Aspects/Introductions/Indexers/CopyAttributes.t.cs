[Introduction]
internal class TargetClass
{
  public global::System.Int32 this[[global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Indexers.CopyAttributes.FooAttribute(3)] global::System.Int32 y, [global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Indexers.CopyAttributes.FooAttribute(4)] global::System.Int32 z]
  {
    [global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Indexers.CopyAttributes.FooAttribute(1)]
    [return: global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Indexers.CopyAttributes.FooAttribute(2)]
    get
    {
      return (global::System.Int32)(42 + y + z);
    }
    [global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Indexers.CopyAttributes.FooAttribute(1)]
    [return: global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Indexers.CopyAttributes.FooAttribute(2)]
    set
    {
      var w = 42 + y + z;
    }
  }
}