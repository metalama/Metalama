// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Desktop.Windows;
using System;

namespace Metalama.Backstage;

/// <summary>
/// The entry point of the desktop notifier of Metalama, which binds the notifier library to the Metalama product.
/// </summary>
internal static class Program
{
    [STAThread]
    public static int Main( string[] args ) => BackstageDesktopProgram.Run( args, new MetalamaDesktopApplicationInfo() );
}
