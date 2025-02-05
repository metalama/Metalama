private async Task<int> Method(int a)
{
  var x = default(global::System.Int32);
  x = await this.Method(a);
  x += await this.Method(a);
  x *= await this.Method(a);
  return default;
}