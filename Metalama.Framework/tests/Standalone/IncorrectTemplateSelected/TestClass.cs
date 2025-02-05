namespace MetaLamaTest;

public sealed class NotNullCheckAttributeTests
{
    [NotNullCheck]
    public static int NormalMethod(string one, string two)
    {
        return 5;
    }

    [NotNullCheck]
    public static async Task<int> AsyncMethod(string one, string two)
    {
        await Task.Yield();
        return 5;
    }

    [NotNullCheck]
    public static IEnumerable<int> EnumerableMethod(string one, string two)
    {
        yield return 1;
        yield return 2;
        yield return 3;
    }

    [NotNullCheck]
    public static IEnumerator<int> EnumeratorMethod(string one, string two)
    {
        yield return 1;
        yield return 2;
        yield return 3;
    }

    [NotNullCheck]
    public static async IAsyncEnumerable<int> AsyncEnumerableMethod(string one, string two)
    {
        await Task.Yield();
        yield return 1;
        yield return 2;
        yield return 3;
    }
}