// Warning TEST0003 on `Increment`: `The redirection of 'other.Increment( 2 )' was refused with ArgumentException: The receiver of 'other.Increment( 2 )' is not of the declaring type of the target method 'Program.Increment_Interceptor(int)' or of a type derived from it.`
// Warning TEST0003 on `Increment`: `The redirection of 'base.Increment( 3 )' was refused with ArgumentException: The base call 'base.Increment( 3 )' cannot be redirected with the receiver mode Receiver.`
[Redirect]
internal class Program : Counter
{
  public void Run(Counter other)
  {
    this.Increment_Interceptor(1);
    other.Increment(2);
    base.Increment(3);
  }
  private void Increment_Interceptor(global::System.Int32 amount)
  {
    this.Increment(amount);
    return;
  }
}