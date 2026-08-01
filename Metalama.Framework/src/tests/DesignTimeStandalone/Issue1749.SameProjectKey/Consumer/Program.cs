// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Aspects2;

namespace Consumer;

/// <summary>
/// Derives from the base class of <c>Aspects2</c>, so that the design-time pipeline of this project has to process
/// the reference to <c>Aspects2</c> and therefore to run the pipeline cached under its <c>ProjectKey</c>.
/// </summary>
internal static class Program
{
    private static void Main() => System.Console.WriteLine( new Derived2().GetMessage2() );
}

public partial class Derived2 : BaseClass2 { }
