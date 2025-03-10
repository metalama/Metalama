// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Samples.Builder;

#pragma warning disable CS0618

[CompileTime]
internal static class BuilderDiagnosticDefinitions
{
    public static readonly DiagnosticDefinition<INamedType>
        BaseTypeCannotContainMoreThanOneBuilderType
            = new(
                "BUILDER01",
                Severity.Error,
                "The type '{0}' cannot contain more than one nested type named 'Builder'.",
                "The base type cannot contain more than one nested type named 'Builder'." );

    public static readonly DiagnosticDefinition<INamedType> BaseTypeMustContainABuilderType
        = new(
            "BUILDER02",
            Severity.Error,
            "The type '{0}' must contain a 'Builder' nested type.",
            "The base type cannot contain more than one builder type." );

    public static readonly DiagnosticDefinition<(INamedType, string)> BaseBuilderMustContainProperty
        = new(
            "BUILDER03",
            Severity.Error,
            "The '{0}' type must contain a property named '{1}'.",
            "The base builder type must contain properties for all properties of the base built type." );

    public static readonly DiagnosticDefinition<(INamedType, int)> BaseTypeMustContainOneConstructor
        = new(
            "BUILDER04",
            Severity.Error,
            "The '{0}' type must contain a single constructor but has {1}.",
            "The base type must contain a single constructor." );

    public static readonly DiagnosticDefinition<(IConstructor, string)>
        BaseTypeConstructorHasUnexpectedParameter
            = new(
                "BUILDER05",
                Severity.Error,
                "The '{1}' parameter of '{0}' cannot be mapped to a property.",
                "A parameter of the base type cannot be mapped to a property." );

    public static readonly DiagnosticDefinition<(INamedType BuilderType, INamedType SourceType)> BaseBuilderMustContainCopyConstructor
        = new(
            "BUILDER07",
            Severity.Error,
            "The '{0}' type must contain a constructor, called the copy constructor, with a single parameter of type '{1}'.",
            "The base type must contain a copy constructor." );

    public static readonly DiagnosticDefinition<(INamedType, int)> BaseBuilderMustContainOneNonCopyConstructor
        = new(
            "BUILDER08",
            Severity.Error,
            "The '{0}' type must contain exactly two constructors but has {1}.",
            "The base builder type must contain exactly two constructors." );
}

[Inheritable]
public class GenerateBuilderAttribute : TypeAspect
{
    [CompileTime]
    private class PropertyMapping
    {
        public PropertyMapping( IProperty sourceProperty, bool isRequired, bool isInherited )
        {
            SourceProperty = sourceProperty;
            IsRequired = isRequired;
            IsInherited = isInherited;
        }

        public IProperty SourceProperty { get; }

        public bool IsRequired { get; }

        public bool IsInherited { get; }

        public IProperty? BuilderProperty { get; set; }

        public int? SourceConstructorParameterIndex { get; set; }

        public int? BuilderConstructorParameterIndex { get; set; }
    }

    [CompileTime]
    private record Tags(
        IReadOnlyList<PropertyMapping> Properties,
        IConstructor SourceConstructor,
        IConstructor BuilderCopyConstructor );

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        var hasError = false;

        var sourceType = builder.Target;

        // Find the Builder nested type in the base type.
        INamedType? baseBuilderType = null;

        IConstructor? baseConstructor = null,
                      baseBuilderConstructor = null,
                      baseBuilderCopyConstructor = null;

        if (sourceType.BaseType != null && sourceType.BaseType.SpecialType != SpecialType.Object)
        {
            // We need to filter parameters to work around a bug where the Constructors collection
            // contains the implicit constructor.
            var baseTypeConstructors =
                sourceType.BaseType.Constructors.Where( c => c.Parameters.Count > 0 ).ToList();

            if (baseTypeConstructors.Count != 1)
            {
                builder.Diagnostics.Report(
                    BuilderDiagnosticDefinitions.BaseTypeMustContainOneConstructor.WithArguments(
                        (
                            sourceType.BaseType, baseTypeConstructors.Count ) ) );

                hasError = true;
            }
            else
            {
                baseConstructor = baseTypeConstructors[0];
            }

            var baseBuilderTypes =
                sourceType.BaseType.Types.OfName( "Builder" ).ToList();

            switch (baseBuilderTypes.Count)
            {
                case 0:
                    builder.Diagnostics.Report( BuilderDiagnosticDefinitions.BaseTypeMustContainABuilderType.WithArguments( sourceType.BaseType.Definition ) );

                    return;

                case > 1:
                    builder.Diagnostics.Report(
                        BuilderDiagnosticDefinitions.BaseTypeCannotContainMoreThanOneBuilderType
                            .WithArguments( sourceType.BaseType.Definition ) );

                    return;

                default:
                    baseBuilderType = baseBuilderTypes[0];

                    // Check that we have exactly two constructors.
                    if (baseBuilderType.Constructors.Count != 2)
                    {
                        builder.Diagnostics.Report(
                            BuilderDiagnosticDefinitions.BaseBuilderMustContainOneNonCopyConstructor
                                .WithArguments(
                                    ( baseBuilderType,
                                      baseBuilderType.Constructors.Count ) ) );

                        return;
                    }

                    // Find the copy constructor.
                    baseBuilderCopyConstructor = baseBuilderType.Constructors
                        .SingleOrDefault(
                            c =>
                                c.Parameters.Count == 1 &&
                                c.Parameters[0].Type == sourceType.BaseType );

                    if (baseBuilderCopyConstructor == null)
                    {
                        builder.Diagnostics.Report(
                            BuilderDiagnosticDefinitions.BaseBuilderMustContainCopyConstructor
                                .WithArguments(
                                    ( baseBuilderType,
                                      sourceType.BaseType ) ) );

                        return;
                    }

                    // The normal constructor is the other constructor.
                    baseBuilderConstructor =
                        baseBuilderType.Constructors.Single( c => c != baseBuilderCopyConstructor );

                    break;
            }
        }

        if (hasError)
        {
            return;
        }

        // Create a list of PropertyMapping items for all properties that we want to build using the Builder.
        var properties = sourceType.AllProperties.Where(
                p => p.Writeability != Writeability.None &&
                     !p.IsStatic )
            .Select(
                p =>
                {
                    var isInherited = p.DeclaringType != sourceType;

                    return new PropertyMapping( p, false, isInherited );
                } )
            .ToList();

        // Introduce the Builder nested type.
        var builderType = builder.IntroduceClass(
            "Builder",
            OverrideStrategy.New,
            t =>
            {
                t.Accessibility = Accessibility.Public;
                t.BaseType = baseBuilderType;
                t.IsSealed = sourceType.IsSealed;
            } );

        // Add builder properties and update the mapping.
        foreach (var property in properties)
        {
            if (property.SourceProperty.DeclaringType == sourceType)
            {
                // For properties of the current type, introduce a new property.
                property.BuilderProperty =
                    builderType.IntroduceAutomaticProperty(
                            property.SourceProperty.Name,
                            property.SourceProperty.Type,
                            IntroductionScope.Instance,
                            buildProperty: p =>
                            {
                                p.Accessibility = Accessibility.Public;

                                p.InitializerExpression =
                                    property.SourceProperty.InitializerExpression;
                            } )
                        .Declaration;
            }
            else if (baseBuilderType != null)
            {
                // For properties of the base type, find the matching property.
                var baseProperty =
                    baseBuilderType.AllProperties.OfName( property.SourceProperty.Name )
                        .SingleOrDefault();

                if (baseProperty == null)
                {
                    builder.Diagnostics.Report(
                        BuilderDiagnosticDefinitions.BaseBuilderMustContainProperty.WithArguments(
                            (
                                baseBuilderType, property.SourceProperty.Name ) ) );

                    hasError = true;
                }
                else
                {
                    property.BuilderProperty = baseProperty;
                }
            }
        }

        if (hasError)
        {
            return;
        }

        // Add a builder constructor accepting the required properties and update the mapping.
        builderType.IntroduceConstructor(
            nameof(BuilderConstructorTemplate),
            buildConstructor: c =>
            {
                c.Accessibility = Accessibility.Public;

                // Adding parameters.
                foreach (var property in properties.Where( m => m.IsRequired ))
                {
                    var parameter = c.AddParameter(
                        NameHelper.ToParameterName( property.SourceProperty.Name ),
                        property.SourceProperty.Type );

                    property.BuilderConstructorParameterIndex = parameter.Index;
                }

                // Calling the base constructor.
                if (baseBuilderConstructor != null)
                {
                    c.InitializerKind = ConstructorInitializerKind.Base;

                    foreach (var baseConstructorParameter in baseBuilderConstructor.Parameters)
                    {
                        var thisParameter =
                            c.Parameters.SingleOrDefault(
                                p =>
                                    p.Name == baseConstructorParameter.Name );

                        if (thisParameter != null)
                        {
                            c.AddInitializerArgument( thisParameter );
                        }
                        else
                        {
                            builder.Diagnostics.Report(
                                BuilderDiagnosticDefinitions
                                    .BaseTypeConstructorHasUnexpectedParameter.WithArguments(
                                        (
                                            baseBuilderConstructor,
                                            baseConstructorParameter.Name ) ) );

                            hasError = true;
                        }
                    }
                }
            } );

        // Add a builder constructor that creates a copy from the source type.
        var builderCopyConstructor = builderType.IntroduceConstructor(
                nameof(BuilderCopyConstructorTemplate),
                buildConstructor: c =>
                {
                    c.Accessibility = Accessibility.ProtectedInternal;
                    c.Parameters[0].Type = sourceType;

                    if (baseBuilderCopyConstructor != null)
                    {
                        c.InitializerKind = ConstructorInitializerKind.Base;

                        if (baseBuilderType != null)
                        {
                            c.AddInitializerArgument( c.Parameters[0] );
                        }
                    }
                } )
            .Declaration;

        if (hasError)
        {
            return;
        }

        // Add a Build method to the builder.
        builderType.IntroduceMethod(
            nameof(BuildMethodTemplate),
            IntroductionScope.Instance,
            OverrideStrategy.New,
            m =>
            {
                m.Name = "Build";
                m.Accessibility = Accessibility.Public;
                m.ReturnType = sourceType;
            } );

        // Add a constructor to the source type with all properties.
        var sourceConstructor = builder.IntroduceConstructor(
                nameof(SourceConstructorTemplate),
                buildConstructor: c =>
                {
                    c.Accessibility = sourceType.IsSealed
                        ? Accessibility.Private
                        : Accessibility.Protected;

                    foreach (var property in properties)
                    {
                        var parameter = c.AddParameter(
                            NameHelper.ToParameterName( property.SourceProperty.Name ),
                            property.SourceProperty.Type );

                        property.SourceConstructorParameterIndex = parameter.Index;
                    }

                    if (baseConstructor != null)
                    {
                        c.InitializerKind = ConstructorInitializerKind.Base;

                        foreach (var baseConstructorParameter in baseConstructor.Parameters)
                        {
                            var thisParameter = c.Parameters.SingleOrDefault(
                                p =>
                                    p.Name == baseConstructorParameter.Name );

                            if (thisParameter == null)
                            {
                                builder.Diagnostics.Report(
                                    BuilderDiagnosticDefinitions
                                        .BaseTypeConstructorHasUnexpectedParameter.WithArguments(
                                            (
                                                baseConstructor, baseConstructorParameter.Name ) ) );

                                hasError = true;
                            }
                            else
                            {
                                c.AddInitializerArgument( thisParameter );
                            }
                        }
                    }
                } )
            .Declaration;

        // Add a ToBuilder method to the source type.
        builder.IntroduceMethod(
            nameof(ToBuilderMethodTemplate),
            whenExists: OverrideStrategy.Override,
            buildMethod: m =>
            {
                m.Accessibility = Accessibility.Public;
                m.Name = "ToBuilder";
                m.ReturnType = builderType.Declaration;
                m.IsVirtual = !sourceType.IsSealed;
            } );

        builder.Tags = new Tags( properties, sourceConstructor, builderCopyConstructor );
    }

    [Template]
    private void BuilderConstructorTemplate()
    {
        var tags = (Tags)meta.Tags.Source!;

        foreach (var property in tags.Properties.Where(
                     p => p is
                         { IsRequired: true, IsInherited: false } ))
        {
            property.SourceProperty.Value =
                meta.Target.Parameters[property.BuilderConstructorParameterIndex!.Value].Value;
        }
    }

    [Template]
    private void BuilderCopyConstructorTemplate( dynamic source )
    {
        var tags = (Tags)meta.Tags.Source!;

        foreach (var property in tags.Properties.Where( p => !p.IsInherited ))
        {
            property.BuilderProperty!.Value =
                property.SourceProperty.With( (IExpression)source ).Value;
        }
    }

    [Template]
    private void SourceConstructorTemplate()
    {
        var tags = (Tags)meta.Tags.Source!;

        foreach (var property in tags.Properties.Where( p => !p.IsInherited ))
        {
            property.BuilderProperty!.Value =
                meta.Target.Parameters[property.SourceConstructorParameterIndex!.Value].Value;
        }
    }

    [Template]
    private dynamic BuildMethodTemplate()
    {
        var tags = (Tags)meta.Tags.Source!;

        return tags.SourceConstructor.Invoke( tags.Properties.Select( x => x.BuilderProperty! ) )!;
    }

    [Template]
    private dynamic ToBuilderMethodTemplate()
    {
        var tags = (Tags)meta.Tags.Source!;

        return tags.BuilderCopyConstructor.Invoke( meta.This );
    }
}

[CompileTime]
internal static class NameHelper
{
    public static string ToParameterName( string name ) => name[0].ToString().ToLowerInvariant() + name[1..];
}

#pragma warning disable CS8618, CS0618

// <target>
[GenerateBuilder]
public class StringKeyedValue<T>
{
    public T Value { get; }
}

// <target>
public class TaggedKeyValue : StringKeyedValue<string>
{
    public string Tag { get; }
}