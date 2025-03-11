// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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