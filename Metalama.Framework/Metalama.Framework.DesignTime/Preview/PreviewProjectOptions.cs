// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Options;

namespace Metalama.Framework.DesignTime.Preview;

internal sealed class PreviewProjectOptions : ProjectOptionsWrapper
{
    public PreviewProjectOptions( IProjectOptions underlying ) : base( underlying ) { }

    public override CodeFormattingOptions CodeFormattingOptions => CodeFormattingOptions.Formatted;

    public override bool FormatCompileTimeCode => false;
}