// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

/// <summary>
/// Helper for creating implicit extension implementation methods.
/// When a method or property accessor is introduced in an extension block,
/// C# generates a static method in the parent type; this helper creates the corresponding MethodBuilderData.
/// </summary>
internal static class ExtensionImplementationHelper
{
    /// <summary>
    /// Creates an implicit extension implementation method for an extension member.
    /// </summary>
    /// <param name="aspectLayerInstance">The aspect layer instance.</param>
    /// <param name="extensionBlock">The extension block containing the member.</param>
    /// <param name="methodName">The name of the implicit method (e.g., "MethodName" or "get_PropertyName").</param>
    /// <param name="accessibility">The accessibility of the implicit method.</param>
    /// <param name="isSourceMemberStatic">Whether the source member is static.</param>
    /// <param name="sourceParameters">The parameters to copy from the source member (can be empty).</param>
    /// <param name="returnType">The return type of the implicit method.</param>
    /// <param name="compilation">The compilation for resolving types.</param>
    /// <param name="declarationKind">The declaration kind (Method or Operator).</param>
    /// <param name="operatorKind">The operator kind if applicable.</param>
    /// <returns>The MethodBuilderData for the implicit method.</returns>
    public static MethodBuilderData CreateImplicitMethod(
        AspectLayerInstance aspectLayerInstance,
        IExtensionBlock extensionBlock,
        string methodName,
        Accessibility accessibility,
        bool isSourceMemberStatic,
        IReadOnlyList<ParameterBuilderData> sourceParameters,
        IType returnType,
        CompilationModel compilation,
        DeclarationKind declarationKind = DeclarationKind.Method,
        OperatorKind operatorKind = OperatorKind.None )
    {
        var parentType = extensionBlock.DeclaringType;

        var implicitMethodBuilder = new MethodBuilder(
            aspectLayerInstance,
            parentType,
            methodName,
            declarationKind,
            operatorKind )
        {
            // Always static.
            Accessibility = accessibility,
            IsStatic = true,

            // Set the return type.
            ReturnType = returnType
        };

        // Mark as implicitly declared - this is an implicit implementation.
        implicitMethodBuilder.SetImplicitlyDeclared();

        // Copy type parameters from the extension block.
        foreach ( var extTypeParam in extensionBlock.TypeParameters )
        {
            var typeParamBuilder = implicitMethodBuilder.AddTypeParameter( extTypeParam.Name );
            typeParamBuilder.Variance = extTypeParam.Variance;
            typeParamBuilder.HasDefaultConstructorConstraint = extTypeParam.HasDefaultConstructorConstraint;
            typeParamBuilder.TypeKindConstraint = extTypeParam.TypeKindConstraint;
            typeParamBuilder.IsConstraintNullable = extTypeParam.IsConstraintNullable;
            typeParamBuilder.AllowsRefStruct = extTypeParam.AllowsRefStruct;

            // Copy type constraints (e.g., base types and interfaces).
            foreach ( var typeConstraint in extTypeParam.TypeConstraints )
            {
                typeParamBuilder.AddTypeConstraint( typeConstraint );
            }
        }

        // For instance members, add the receiver as the first parameter.
        if ( !isSourceMemberStatic )
        {
            var receiverParam = extensionBlock.ReceiverParameter;

            implicitMethodBuilder.AddParameter(
                receiverParam.Name ?? "self",
                receiverParam.Type,
                receiverParam.RefKind );
        }

        // Copy the source parameters.
        foreach ( var param in sourceParameters )
        {
            var paramType = param.Type.GetTarget( compilation );
            var defaultValue = param.DefaultValue?.ToTypedConstant( compilation );

            implicitMethodBuilder.AddParameter(
                param.Name ?? $"arg{param.Index}",
                paramType,
                param.RefKind,
                defaultValue );
        }

        // Freeze and return the builder data.
        implicitMethodBuilder.Freeze();

        return implicitMethodBuilder.BuilderData;
    }

    /// <summary>
    /// Creates an implicit extension implementation method for a property accessor.
    /// </summary>
    /// <param name="aspectLayerInstance">The aspect layer instance.</param>
    /// <param name="extensionBlock">The extension block containing the property.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="isSetter">True for setter, false for getter.</param>
    /// <param name="accessorAccessibility">The accessibility of the accessor.</param>
    /// <param name="isPropertyStatic">Whether the property is static.</param>
    /// <param name="propertyType">The property type.</param>
    /// <param name="compilation">The compilation for resolving types.</param>
    /// <returns>The MethodBuilderData for the implicit accessor method.</returns>
    public static MethodBuilderData CreateImplicitAccessorMethod(
        AspectLayerInstance aspectLayerInstance,
        IExtensionBlock extensionBlock,
        string propertyName,
        bool isSetter,
        Accessibility accessorAccessibility,
        bool isPropertyStatic,
        IType propertyType,
        ICompilation compilation )
    {
        var parentType = extensionBlock.DeclaringType;
        var methodName = (isSetter ? "set_" : "get_") + propertyName;

        var implicitMethodBuilder = new MethodBuilder(
            aspectLayerInstance,
            parentType,
            methodName )
        {
            // Always static.
            Accessibility = accessorAccessibility,
            IsStatic = true
        };

        implicitMethodBuilder.SetImplicitlyDeclared();

        // Copy type parameters from the extension block.
        foreach ( var extTypeParam in extensionBlock.TypeParameters )
        {
            var typeParamBuilder = implicitMethodBuilder.AddTypeParameter( extTypeParam.Name );
            typeParamBuilder.Variance = extTypeParam.Variance;
            typeParamBuilder.HasDefaultConstructorConstraint = extTypeParam.HasDefaultConstructorConstraint;
            typeParamBuilder.TypeKindConstraint = extTypeParam.TypeKindConstraint;
            typeParamBuilder.IsConstraintNullable = extTypeParam.IsConstraintNullable;
            typeParamBuilder.AllowsRefStruct = extTypeParam.AllowsRefStruct;

            // Copy type constraints (e.g., base types and interfaces).
            foreach ( var typeConstraint in extTypeParam.TypeConstraints )
            {
                typeParamBuilder.AddTypeConstraint( typeConstraint );
            }
        }

        // For instance properties, add the receiver as the first parameter.
        if ( !isPropertyStatic )
        {
            var receiverParam = extensionBlock.ReceiverParameter;

            implicitMethodBuilder.AddParameter(
                receiverParam.Name ?? "self",
                receiverParam.Type,
                receiverParam.RefKind );
        }

        if ( isSetter )
        {
            // Add value parameter for setter.
            implicitMethodBuilder.AddParameter( "value", propertyType );

            // Setter returns void (default).
        }
        else
        {
            // Getter returns the property type.
            implicitMethodBuilder.ReturnType = propertyType;
        }

        // Freeze and return the builder data.
        implicitMethodBuilder.Freeze();

        return implicitMethodBuilder.BuilderData;
    }
}

#endif