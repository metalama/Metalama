private void Method(int x)
{
  switch (x)
  {
    case 42:
      this.Method(0);
      break;
  }
  this.Method(x);
  return;
}