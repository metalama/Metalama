class TargetClass
{
    [Aspect]
    public int TargetMethod()
    {
        int a_1, b_1, c_1, d_1, e_1, f_1, g_1, h_1, i_1, j_1, k_1, l_1, m_1, n_1;
        var a = 0;
        var (b, _) = (1, 2);
        (var c, var _, _) = (3, 4, 5);
        int.TryParse("1", out var d);
        int.TryParse("2", out var _);
        int.TryParse("3", out _);
        _ =
          from e in new int[0]
          let f = e
          join g in new int[0] on e equals g
          join h in new int[0] on f equals h into i
          group e by e into j
          select j into k
          select k;
    l:
        ;
        foreach (var m in new int[]
        {
        }
        )
        {
        }
        try
        {
        }
        catch (Exception)
        {
        }
        try
        {
        }
        catch (Exception n)
        {
        }
        return 0;
    }
}