// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Aspects1;

/// <summary>
/// An aspect that exists only so that this project has compile-time code and therefore a design-time pipeline.
/// </summary>
/// <remarks>
/// Nothing references this project. Its role is to be the first project analyzed under its <c>ProjectKey</c>, so
/// that the pipeline cached under that key holds a compilation of <em>this</em> project, whose reference list
/// contains the two <c>Contract</c> assemblies.
/// </remarks>
[Inheritable]
public class Aspect1 : TypeAspect
{
    [Introduce( WhenExists = OverrideStrategy.New )]
    public string GetMessage1() => "Aspects1";
}

/// <summary>
/// The target of <see cref="Aspect1"/>.
/// </summary>
[Aspect1]
public partial class BaseClass1 { }
