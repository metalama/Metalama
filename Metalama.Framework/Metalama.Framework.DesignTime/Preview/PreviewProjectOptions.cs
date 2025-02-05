// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Options;

namespace Metalama.Framework.DesignTime.Preview;

internal sealed class PreviewProjectOptions : ProjectOptionsWrapper
{
    public PreviewProjectOptions( IProjectOptions underlying ) : base( underlying ) { }

    public override CodeFormattingOptions CodeFormattingOptions => CodeFormattingOptions.Formatted;

    public override bool FormatCompileTimeCode => false;
}