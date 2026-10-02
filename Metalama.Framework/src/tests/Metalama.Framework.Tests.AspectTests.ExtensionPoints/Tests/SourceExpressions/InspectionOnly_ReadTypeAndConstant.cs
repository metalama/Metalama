// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.SourceExpressions.InspectionOnly_ReadTypeAndConstant;

// An extension gives compile-time code the initializer of a field as an inspection-only expression. The aspect reads its text, its type and
// its constant value.

internal class InspectAttribute : TypeAspect
{
    private static readonly DiagnosticDefinition<(string Text, IType Type, string Constant)> _description =
        new( "MY001", Severity.Warning, "Initializer '{0}' of type '{1}', constant value: {2}." );

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var field in builder.Target.Fields.Where( f => f.InitializerExpression != null ).OrderBy( f => f.Name ) )
        {
            var expression = (ISourceExpression) field.TestGetInspectionOnlyInitializer();
            var constant = expression.AsTypedConstant?.Value?.ToString() ?? "none";

            builder.With( field ).Diagnostics.Report( _description.WithArguments( (expression.AsString, expression.Type, constant) ) );
        }
    }
}

// <target>
[Inspect]
internal class Target
{
    public int A = 42;
    public string B = "text";
    public int C = 1 + 2;
}
