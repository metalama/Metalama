// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Linq;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

internal static class OverrideHelper
{
    public static IProperty OverrideProperty(
        ProjectServiceProvider serviceProvider,
        AspectLayerInstance aspectLayerInstance,
        IFieldOrPropertyOrIndexer targetDeclaration,
        BoundTemplateMethod? getTemplate,
        BoundTemplateMethod? setTemplate,
        Action<ITransformation> addTransformation,
        CompilationModel? mutableCompilation = null )
    {
        // Determine if any template uses the 'field' keyword and needs a backing field.
        var introducesBackingField =
            (getTemplate?.TemplateMember.IntroducesBackingField ?? false) ||
            (setTemplate?.TemplateMember.IntroducesBackingField ?? false);

        // Determine if any template assigns to the 'field' keyword (vs just reading it).
        var isBackingFieldAssigned =
            (getTemplate?.TemplateMember.IsBackingFieldAssigned ?? false) ||
            (setTemplate?.TemplateMember.IsBackingFieldAssigned ?? false);

        string? backingFieldName = null;

        if ( introducesBackingField )
        {
            Invariant.Assert( mutableCompilation != null, "mutableCompilation is required when template introduces a backing field" );

            // Compute a unique backing field name using the mutable compilation for collision checking.
            var property = targetDeclaration switch
            {
                IField { OverridingProperty: { } overridingProperty } => overridingProperty,
                IField => null, // Will be computed later after promotion
                IProperty p => p,
                _ => null
            };

            if ( property != null )
            {
                backingFieldName = ComputeBackingFieldName( property, mutableCompilation! );
            }
        }

        switch ( targetDeclaration )
        {
            case IField { OverridingProperty: { } overridingProperty }:
                return OverrideProperty(
                    serviceProvider,
                    aspectLayerInstance,
                    overridingProperty,
                    getTemplate,
                    setTemplate,
                    addTransformation,
                    mutableCompilation );

            case IField field:
                {
                    var transformation = PromoteFieldTransformation.Create( serviceProvider, field, aspectLayerInstance );

                    addTransformation( transformation );

                    // If we need a backing field for a field being promoted, compute the name now.
                    if ( introducesBackingField && backingFieldName == null )
                    {
                        backingFieldName = ComputeBackingFieldName( transformation.OverridingProperty, mutableCompilation! );
                    }

                    // Introduce the backing field if needed.
                    if ( introducesBackingField && backingFieldName != null )
                    {
                        IntroduceBackingField(
                            aspectLayerInstance,
                            transformation.OverridingProperty,
                            backingFieldName,
                            isBackingFieldAssigned,
                            addTransformation );
                    }

                    addTransformation(
                        new OverridePropertyTransformation(
                            aspectLayerInstance,
                            transformation.OverridingProperty.ToRef(),
                            getTemplate,
                            setTemplate,
                            backingFieldName ) );

                    AddTransformationsForStructField( targetDeclaration.DeclaringType, aspectLayerInstance, addTransformation );

                    return transformation.OverridingProperty;
                }

            case IProperty property:
                {
                    // Introduce the backing field if needed.
                    if ( introducesBackingField && backingFieldName != null )
                    {
                        IntroduceBackingField(
                            aspectLayerInstance,
                            property,
                            backingFieldName,
                            isBackingFieldAssigned,
                            addTransformation );
                    }

                    addTransformation(
                        new OverridePropertyTransformation( aspectLayerInstance, property.ToFullRef(), getTemplate, setTemplate, backingFieldName ) );

                    if ( property.IsAutoPropertyOrField.GetValueOrDefault() )
                    {
                        AddTransformationsForStructField( targetDeclaration.DeclaringType, aspectLayerInstance, addTransformation );
                    }

                    return property;
                }

            default:
                throw new AssertionFailedException( $"Unexpected declaration: '{targetDeclaration}'." );
        }
    }

    /// <summary>
    /// Computes a unique backing field name for a property that uses the <c>field</c> keyword in its template.
    /// </summary>
    internal static string ComputeBackingFieldName( IProperty property, CompilationModel compilation )
    {
        var propertyName = property.Name;
        var camelCaseName = propertyName.ToCamelCase();
        var hint = camelCaseName.StartsWith( "_", StringComparison.Ordinal ) ? camelCaseName : "_" + camelCaseName;

        // Check for collisions and make the name unique.
        var containingType = property.DeclaringType.ForCompilation( compilation );

        if ( !HasMemberWithName( containingType, hint ) )
        {
            return hint;
        }

        for ( var i = 1; i < int.MaxValue; i++ )
        {
            var candidate = hint + i;

            if ( !HasMemberWithName( containingType, candidate ) )
            {
                return candidate;
            }
        }

        throw new InvalidOperationException( $"Cannot compute a unique backing field name for property '{property.Name}'." );

        static bool HasMemberWithName( INamedType type, string name )
            => type.AllFields.OfName( name ).Any()
               || type.AllProperties.OfName( name ).Any()
               || type.AllEvents.OfName( name ).Any()
               || type.AllMethods.OfName( name ).Any();
    }

    /// <summary>
    /// Introduces a backing field for a property template that uses the <c>field</c> keyword.
    /// </summary>
    /// <param name="aspectLayerInstance">The aspect layer instance.</param>
    /// <param name="property">The property to introduce a backing field for.</param>
    /// <param name="backingFieldName">The name of the backing field.</param>
    /// <param name="isBackingFieldAssigned">
    /// Whether the template assigns to the backing field. When <c>true</c>, the field is writable;
    /// when <c>false</c>, the field can be readonly (ConstructorOnly).
    /// </param>
    /// <param name="addTransformation">Action to add transformations.</param>
    internal static void IntroduceBackingField(
        AspectLayerInstance aspectLayerInstance,
        IProperty property,
        string backingFieldName,
        bool isBackingFieldAssigned,
        Action<ITransformation> addTransformation )
    {
        var fieldBuilder = new FieldBuilder( aspectLayerInstance, property.DeclaringType, backingFieldName )
        {
            Accessibility = Accessibility.Private,
            Type = property.Type,
            IsStatic = property.IsStatic,

            // The backing field is writable only if the template actually assigns to it.
            // If the template only reads from 'field', the backing field can be readonly.
            Writeability = isBackingFieldAssigned ? Writeability.All : Writeability.ConstructorOnly,

            // Transfer the property initializer to the backing field.
            InitializerExpression = property.InitializerExpression
        };

        fieldBuilder.Freeze();

        addTransformation( fieldBuilder.CreateTransformation() );
    }

    public static void AddTransformationsForStructField( INamedType type, AspectLayerInstance aspectLayerInstance, Action<ITransformation> addTransformation )
    {
        if ( type.TypeKind is TypeKind.Struct )
        {
            // If there is no 'this()' constructor, add one.
            if ( type.Constructors.FirstOrDefault() is { IsImplicitlyDeclared: true } implicitConstructor )
            {
                var constructorBuilder = new ConstructorBuilder( aspectLayerInstance, implicitConstructor );

                constructorBuilder.Freeze();

                addTransformation( constructorBuilder.CreateTransformation() );
            }
        }
    }
}