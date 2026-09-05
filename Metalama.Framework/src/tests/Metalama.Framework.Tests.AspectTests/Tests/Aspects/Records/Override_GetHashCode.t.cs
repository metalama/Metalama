[Override]
internal record Target(int X)
{
  public override global::System.Int32 GetHashCode()
  {
    // <target>
    global::System.Console.WriteLine("Overridden!");
    return unchecked(((global::System.Collections.Generic.EqualityComparer<global::System.Type>.Default.GetHashCode(this.EqualityContract)) * -1521134295) + global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.GetHashCode(this.X));
  }
}