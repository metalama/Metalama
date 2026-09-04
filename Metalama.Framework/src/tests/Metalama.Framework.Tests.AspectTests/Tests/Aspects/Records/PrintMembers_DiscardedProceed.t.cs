[Override]
internal record DerivedRecord : BaseRecord
{
  public DerivedRecord(int x) : base(x)
  {
  }
  protected override global::System.Boolean PrintMembers(global::System.Text.StringBuilder builder)
  {
    // <target>
    _ = base.PrintMembers(builder);
    return (global::System.Boolean)true;
  }
}