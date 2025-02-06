// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Wpf;
using System.Windows;

namespace Doc.Command.CanExecute_Czech;

public class MojeOkno : Window
{
    public int Počitadlo { get; private set; }

    [Command]
    public void VykonatZvýšení()
    {
        this.Počitadlo++;
    }

    public bool MůžemeVykonatZvýšení => this.Počitadlo < 10;

    [Command]
    public void Snížit()
    {
        this.Počitadlo--;
    }

    public bool MůžemeSnížit => this.Počitadlo > 0;
}