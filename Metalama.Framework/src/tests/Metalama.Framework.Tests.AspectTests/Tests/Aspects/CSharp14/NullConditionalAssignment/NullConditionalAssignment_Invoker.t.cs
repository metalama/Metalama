internal class C
{
    private C? _p;
    [TheAspect]
    public C? P
    {
        get
        {
            return this._p;
        }

        set
        {
            this._p?.P = null;
        }
    }
}
