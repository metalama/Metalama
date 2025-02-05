{
    global::System.Int32 result;
    result = this.Method(a);
    switch (result)
    {
        case string s:
            global::System.Console.WriteLine(s);
            break;
        case int i when i < 0:
            throw new global::System.IndexOutOfRangeException();
        case var x:
            break;
    }

    return (int)result;
}