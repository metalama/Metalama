// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.Classification;
using Metalama.Framework.Engine.Formatting;

namespace Metalama.Framework.DesignTime.VisualStudio.Classification;

internal static class DesignTimeTextSpanClassificationHelper
{
    public static DesignTimeTextSpanClassification ToDesignTime( this TextSpanClassification classification )
        => classification switch
        {
            TextSpanClassification.Conflict => DesignTimeTextSpanClassification.Conflict,
            TextSpanClassification.Default => DesignTimeTextSpanClassification.Default,
            TextSpanClassification.Dynamic => DesignTimeTextSpanClassification.Dynamic,
            TextSpanClassification.Excluded => DesignTimeTextSpanClassification.Excluded,
            TextSpanClassification.CompileTime => DesignTimeTextSpanClassification.CompileTime,
            TextSpanClassification.GeneratedCode => DesignTimeTextSpanClassification.GeneratedCode,
            TextSpanClassification.RunTime => DesignTimeTextSpanClassification.RunTime,
            TextSpanClassification.SourceCode => DesignTimeTextSpanClassification.SourceCode,
            TextSpanClassification.TemplateKeyword => DesignTimeTextSpanClassification.TemplateKeyword,
            TextSpanClassification.CompileTimeVariable => DesignTimeTextSpanClassification.CompileTimeVariable,
            TextSpanClassification.NeutralTrivia => DesignTimeTextSpanClassification.Default,
            _ => throw new ArgumentOutOfRangeException( nameof(classification), classification, null )
        };
}