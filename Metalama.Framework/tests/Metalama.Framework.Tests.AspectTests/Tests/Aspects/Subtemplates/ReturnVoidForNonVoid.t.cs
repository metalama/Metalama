// Final Compilation.Emit failed.
// Error CS0126  on `return`: `An object of a type convertible to 'int' is required`
[Aspect]
private int Method()
{
  return;
  return default;
}