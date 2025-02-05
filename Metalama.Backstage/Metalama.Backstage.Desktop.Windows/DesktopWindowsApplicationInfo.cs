// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;

namespace Metalama.Backstage.Desktop.Windows;

internal class DesktopWindowsApplicationInfo : ApplicationInfoBase
{
    public DesktopWindowsApplicationInfo() : base( typeof(DesktopWindowsApplicationInfo).Assembly ) { }

    public override string Name => "Metalama.Backstage.Desktop.Windows";

    public override ProcessKind ProcessKind => ProcessKind.BackstageDesktopWindows;
}