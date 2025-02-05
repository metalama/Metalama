private async Task Method(int a, int b)
{
  var result = this.Method(a, b);
  await result;
  return;
}