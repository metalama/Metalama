internal class Foo
{
  public void Method1([Trim] string nonNullableString, [Trim] string? nullableString)
  {
    nonNullableString = nonNullableString.Trim();
    nullableString = nullableString?.Trim();
    Console.WriteLine($"nonNullableString='{nonNullableString}', nullableString='{nullableString}'");
  }
  public string? Property { get; set; }
}