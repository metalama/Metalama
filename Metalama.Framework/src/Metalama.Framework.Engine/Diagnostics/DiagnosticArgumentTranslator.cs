// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.CodeModel;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Engine.Diagnostics;

/// <summary>
/// Translates the declarations in the arguments of a diagnostic into another compilation, so that the diagnostic message
/// displays these declarations as they are in that compilation.
/// </summary>
/// <remarks>
/// Eligibility is evaluated against the current compilation, which contains the changes of the aspects that ran before.
/// For instance, a constructor can contain a parameter that an aspect introduced. The diagnostic must display the declarations
/// as the user wrote them, so the arguments are translated into the initial compilation. A declaration that does not exist
/// in the initial compilation is kept unchanged.
/// </remarks>
internal static class DiagnosticArgumentTranslator
{
    /// <summary>
    /// Gets the declaration that represents a given declaration in a given compilation, or the given declaration
    /// if it does not exist in this compilation.
    /// </summary>
    public static T Translate<T>( T declaration, CompilationModel compilation )
        where T : class, IDeclaration
        => declaration.TryForCompilation( compilation, out var translated ) ? translated : declaration;

    /// <summary>
    /// Gets a copy of a <see cref="FormattableString"/> in which the declarations of the arguments, including the declarations
    /// of nested <see cref="FormattableString"/> and <see cref="IDescribedObject{T}"/> arguments, are translated into a given compilation.
    /// </summary>
    public static FormattableString Translate( FormattableString formattableString, CompilationModel compilation )
        => FormattableStringFactory.Create(
            formattableString.Format,
            formattableString.GetArguments().SelectAsArray( a => TranslateArgument( a, compilation ) ) );

    private static object? TranslateArgument( object? argument, CompilationModel compilation )
        => argument switch
        {
            IDeclaration declaration => Translate( declaration, compilation ),
            FormattableString formattableString => Translate( formattableString, compilation ),
            IDescribedObject<object?> describedObject => new DescribedObject<object?>(
                TranslateArgument( describedObject.Object, compilation ),
                describedObject.Description == null ? null : Translate( describedObject.Description, compilation ) ),
            _ => argument
        };
}
