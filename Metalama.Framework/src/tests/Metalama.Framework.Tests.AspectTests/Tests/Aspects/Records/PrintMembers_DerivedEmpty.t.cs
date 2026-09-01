[Override]
internal record DerivedRecord : BaseRecord
{
  public DerivedRecord(int x) : base(x)
  {
  }
  protected override global::System.Boolean PrintMembers(global::System.Text.StringBuilder builder)
  {
    // <target>
    return base.PrintMembers(builder);
  }
}