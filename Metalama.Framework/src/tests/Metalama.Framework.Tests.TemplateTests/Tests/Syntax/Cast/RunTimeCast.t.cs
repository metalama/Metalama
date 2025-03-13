private string Method(string a)
{
  object arg0 = null;
  arg0 = a;
  if (arg0 is string)
  {
    var s = (string)arg0;
    global::System.Console.WriteLine(s);
  }
  var result = this.Method(a);
  object obj = result;
  var text = obj as string;
  if (text != null)
  {
    return (global::System.String)text.Trim();
  }
  return (global::System.String)obj;
}