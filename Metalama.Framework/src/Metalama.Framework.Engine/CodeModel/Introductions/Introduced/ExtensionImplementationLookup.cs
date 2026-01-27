// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

/// <summary>
/// Helper for finding extension implementation methods for extension members.
/// </summary>
internal static class ExtensionImplementationLookup
{
    /// <summary>
    /// Finds the implicit extension implementation method for an extension member.
    /// </summary>
    /// <param name="extensionBlock">The extension block containing the member.</param>
    /// <param name="implicitMethodName">The expected name of the implicit method.</param>
    /// <param name="isSourceMemberStatic">Whether the source member is static.</param>
    /// <param name="sourceParameters">The parameters of the source member.</param>
    /// <param name="comparers">The comparers to use for type comparison.</param>
    /// <returns>The implicit implementation method, or null if not found.</returns>
    public static IMethod? FindImplicitMethod(
        IExtensionBlock extensionBlock,
        string implicitMethodName,
        bool isSourceMemberStatic,
        IParameterList sourceParameters,
        ICompilationComparers comparers )
    {
        var parentType = extensionBlock.DeclaringType;
        var receiverType = extensionBlock.ReceiverType;

        // Use OfName for better performance.
        foreach ( var method in parentType.Methods.OfName( implicitMethodName ) )
        {
            // Check if this is an implicit declaration (introduced by us).
            if ( method is IntroducedMethod introducedMethod &&
                 introducedMethod.BuilderData is MethodBuilderData methodData &&
                 methodData.IsImplicitlyDeclared )
            {
                // For static members, signatures should match exactly.
                // For instance members, the implicit method has the receiver as the first parameter.
                if ( isSourceMemberStatic )
                {
                    if ( ParametersMatch( sourceParameters, method.Parameters, comparers ) )
                    {
                        return method;
                    }
                }
                else
                {
                    if ( ParametersMatchWithReceiverOffset( sourceParameters, method.Parameters, receiverType, comparers ) )
                    {
                        return method;
                    }
                }
            }
        }

        return null;
    }

    private static bool ParametersMatch(
        IParameterList sourceParameters,
        IParameterList implicitParameters,
        ICompilationComparers comparers )
    {
        if ( sourceParameters.Count != implicitParameters.Count )
        {
            return false;
        }

        for ( var i = 0; i < sourceParameters.Count; i++ )
        {
            if ( !comparers.Default.Equals( sourceParameters[i].Type, implicitParameters[i].Type ) )
            {
                return false;
            }

            // Also check RefKind - methods can be overloaded based on ref/in/out modifiers.
            if ( sourceParameters[i].RefKind != implicitParameters[i].RefKind )
            {
                return false;
            }
        }

        return true;
    }

    private static bool ParametersMatchWithReceiverOffset(
        IParameterList sourceParameters,
        IParameterList implicitParameters,
        IType receiverType,
        ICompilationComparers comparers )
    {
        // Implicit method should have one more parameter (the receiver).
        if ( implicitParameters.Count != sourceParameters.Count + 1 )
        {
            return false;
        }

        // First parameter should be the receiver type.
        if ( !comparers.Default.Equals( implicitParameters[0].Type, receiverType ) )
        {
            return false;
        }

        // Remaining parameters should match.
        for ( var i = 0; i < sourceParameters.Count; i++ )
        {
            if ( !comparers.Default.Equals( sourceParameters[i].Type, implicitParameters[i + 1].Type ) )
            {
                return false;
            }

            // Also check RefKind - methods can be overloaded based on ref/in/out modifiers.
            if ( sourceParameters[i].RefKind != implicitParameters[i + 1].RefKind )
            {
                return false;
            }
        }

        return true;
    }
}

#endif