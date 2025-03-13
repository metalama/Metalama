// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;

namespace Metalama.Backstage.Desktop.Windows;

internal class DesktopWindowsApplicationInfo : ApplicationInfoBase
{
    public DesktopWindowsApplicationInfo() : base( typeof(DesktopWindowsApplicationInfo).Assembly ) { }

    public override string Name => "Metalama.Backstage.Desktop.Windows";

    public override ProcessKind ProcessKind => ProcessKind.BackstageDesktopWindows;
}