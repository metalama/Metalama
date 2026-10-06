// Warning MY001 on `A`: `Initializer '42' of type 'int', constant value: 42.`
// Warning MY001 on `B`: `Initializer '"text"' of type 'string', constant value: text.`
// Warning MY001 on `C`: `Initializer '1 + 2' of type 'int', constant value: none.`
[Inspect]
internal class Target
{
  public int A = 42;
  public string B = "text";
  public int C = 1 + 2;
}