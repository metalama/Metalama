// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using RefKind = Metalama.Framework.Code.RefKind;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

/// <summary>
/// Builds the parts of the declaration of an introduced delegate that are specific to that kind.
/// </summary>
internal static class DelegateHelper
{
    /// <summary>
    /// Gets the <c>Invoke</c> method of an introduced delegate, which carries its signature.
    /// </summary>
    public static IMethod GetInvokeMethod( INamedType introducedType ) => introducedType.Facets.Delegate.AssertNotNull().InvokeMethod;

    /// <summary>
    /// Builds the return type of a delegate declaration, including the <c>ref</c> and <c>ref readonly</c> modifiers
    /// of a return by reference, which the declaration writes before the return type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>ContextualSyntaxGenerator.ReturnType</c> is not used, because it emits the type alone. Metalama does not
    /// introduce a method that returns by reference, which is why the setter of <c>ParameterBuilder.RefKind</c>
    /// refuses a return parameter for every kind but a delegate, and a delegate is therefore the one declaration
    /// that needs the modifier emitted here.
    /// </para>
    /// </remarks>
    public static TypeSyntax GetReturnType( INamedType introducedType, MemberInjectionContext context )
    {
        var returnParameter = GetInvokeMethod( introducedType ).ReturnParameter;
        var returnType = context.SyntaxGenerator.TypeSyntax( returnParameter.Type );

        return returnParameter.RefKind switch
        {
            RefKind.None => returnType,
            RefKind.Ref => RefType( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.RefKeyword ), default, returnType ),
            RefKind.RefReadOnly => RefType(
                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.RefKeyword ),
                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ReadOnlyKeyword ),
                returnType ),
            _ => throw new AssertionFailedException(
                $"Unsupported reference kind '{returnParameter.RefKind}' on the return value of the delegate '{introducedType}'." )
        };
    }
}
