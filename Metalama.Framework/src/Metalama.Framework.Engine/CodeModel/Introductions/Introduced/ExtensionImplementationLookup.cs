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
    /// <returns>The implicit implementation method, or null if not found.</returns>
    public static IMethod? FindImplicitMethod(
        IExtensionBlock extensionBlock,
        string implicitMethodName,
        bool isSourceMemberStatic,
        IParameterList sourceParameters )
    {
        var parentType = extensionBlock.DeclaringType;
        var receiverType = extensionBlock.ReceiverType;
        var receiverRefKind = extensionBlock.ReceiverParameter.RefKind;
        var comparers = parentType.Compilation.Comparers;

        // Build the expected parameter signature.
        // For instance members, the implicit method has the receiver as the first parameter.
        var expectedParameterCount = isSourceMemberStatic
            ? sourceParameters.Count
            : sourceParameters.Count + 1;

        // Use SignatureMatcher pattern with OfExactSignature.
        var matchingMethod = parentType.Methods.OfExactSignature(
            (sourceParameters, receiverType, receiverRefKind, isSourceMemberStatic),
            implicitMethodName,
            expectedParameterCount,
            GetExpectedParameter,
            isStatic: true );

        // Verify it's an implicit declaration (introduced by us).
        if ( matchingMethod is IntroducedMethod { BuilderData: MethodBuilderData { IsImplicitlyDeclared: true } } )
        {
            return matchingMethod;
        }

        // If no exact match found via SignatureMatcher, search manually for implicit methods.
        // This handles the case where SignatureMatcher returns a non-implicit method.
        foreach ( var method in parentType.Methods.OfName( implicitMethodName ) )
        {
            if ( method is IntroducedMethod { BuilderData: MethodBuilderData { IsImplicitlyDeclared: true } } &&
                 method.Parameters.Count == expectedParameterCount &&
                 ParametersMatch( method.Parameters, sourceParameters, receiverType, receiverRefKind, isSourceMemberStatic, comparers ) )
            {
                return method;
            }
        }

        return null;

        static (IType Type, RefKind RefKind) GetExpectedParameter(
            (IParameterList SourceParameters, IType ReceiverType, RefKind ReceiverRefKind, bool IsStatic) payload,
            int index )
        {
            if ( !payload.IsStatic && index == 0 )
            {
                // First parameter is the receiver for instance members.
                return (payload.ReceiverType, payload.ReceiverRefKind);
            }

            var sourceIndex = payload.IsStatic ? index : index - 1;
            var param = payload.SourceParameters[sourceIndex];

            return (param.Type, param.RefKind);
        }
    }

    private static bool ParametersMatch(
        IParameterList implicitParameters,
        IParameterList sourceParameters,
        IType receiverType,
        RefKind receiverRefKind,
        bool isSourceMemberStatic,
        ICompilationComparers comparers )
    {
        var offset = isSourceMemberStatic ? 0 : 1;

        // For instance members, verify the receiver parameter.
        if ( !isSourceMemberStatic )
        {
            if ( !comparers.Default.Equals( implicitParameters[0].Type, receiverType ) ||
                 implicitParameters[0].RefKind != receiverRefKind )
            {
                return false;
            }
        }

        // Verify remaining parameters.
        for ( var i = 0; i < sourceParameters.Count; i++ )
        {
            if ( !comparers.Default.Equals( implicitParameters[i + offset].Type, sourceParameters[i].Type ) ||
                 implicitParameters[i + offset].RefKind != sourceParameters[i].RefKind )
            {
                return false;
            }
        }

        return true;
    }
}

#endif