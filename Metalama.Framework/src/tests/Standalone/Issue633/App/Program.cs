// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// Regression test for #633: Application references a library that uses
// Metalama.Patterns.Contracts with PrivateAssets="all".
// The build should succeed without "Cannot find the compile-time assembly" errors.

using Issue633.Library;

var validated = new Validated();
Console.WriteLine( validated.GetGreeting( "World" ) );
Console.WriteLine( validated.Clamp( 42 ) );
