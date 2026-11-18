[AddAnnotationAspect]
internal class C
{
}
[ReadAnnotationAspect]
internal class D
{
  public C? Nullable;
  public C NonNullable = new();
#nullable disable
  public C Oblivious;
  private global::System.String ReadNonNullable = "TheValue";
  private global::System.String ReadNullable = "TheValue";
  private global::System.String ReadOblivious = "TheValue";
#nullable enable
}