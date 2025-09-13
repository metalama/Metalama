public class SomeDisposable : ISomeInterface
{
#if TESTRUNNER
        void Foo()
        {
            Debug.Fail("");
        }
#endif
#if !TESTRUNNER
  private void Bar()
  {
    Debug.Fail("");
  }
#endif
}