using Middle1;
using Middle2;

namespace Dependent;

internal class Program
{
    static void Main(string[] args)
    {
        var test1 = new Test1();
        var test2 = new Test2();
    }
}

public class Test1 : BaseClass1
{
}

public class Test2 : BaseClass2
{
}