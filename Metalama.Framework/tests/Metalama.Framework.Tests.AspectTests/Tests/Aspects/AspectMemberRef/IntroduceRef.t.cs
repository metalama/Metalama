
// <target>
class Program
{
    [Retry]
    static int Foo(int a)
    {
        this.Introduced("Foo");
        return 0;
    }


    private void Introduced(global::System.String name)
    {
    }
}

