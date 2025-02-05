using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Samples.EnumViewModel2;

#pragma warning disable CS0649

public class EnumViewModelAttribute : TypeAspect
{
    private static readonly DiagnosticDefinition<INamedType> _missingFieldError =
        new(
            "ENUM01",
            Severity.Error,
            "The [EnumViewModel] aspect requires the type '{0}' to have a field named '_value'." );

    private static readonly SuppressionDefinition _suppression = new( "IDE0052" );

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var enumType = builder.Target.Types.Single();

        var viewModelType = builder.IntroduceClass(
                enumType.Name + "ViewModel",
                buildType: b => { b.Accessibility = Accessibility.Public; } )
            .Declaration;

        var valueField =
            builder.With( viewModelType )
                .IntroduceField(
                    nameof(FieldTemplate),
                    buildField: b =>
                    {
                        b.Name = "_value";
                        b.Writeability = Writeability.ConstructorOnly;
                        b.Type = enumType;
                    } )
                .Declaration;

        var constructor =
            builder.With( viewModelType )
                .IntroduceConstructor(
                    nameof(ConstructorTemplate),
                    buildConstructor: b =>
                    {
                        b.Accessibility = Accessibility.Public;
                        b.AddParameter( "value", enumType );
                    },
                    args: new { valueField = valueField } )
                .Declaration;

        if (valueField == null)
        {
            // If the field does not exist, emit an error.
            builder.Diagnostics.Report( _missingFieldError.WithArguments( builder.Target ) );

            return;
        }

        // Suppress the IDE0052 warning telling that the field is assigned but not read. We get this at design time
        // because the partial class does not contain the member implementations.
        builder.Diagnostics.Suppress( _suppression, valueField );

        // Get the field type and decides the template.
        var isFlags = enumType.Attributes.Any( a => a.Type.IsConvertibleTo( typeof(FlagsAttribute) ) );
        var template = isFlags ? nameof(IsFlagTemplate) : nameof(IsMemberTemplate);

        // Introduce a property into the view-model type for each enum member.
        foreach (var member in enumType.Fields)
        {
            builder.With( viewModelType )
                .IntroduceProperty(
                    template,
                    tags: new { member },
                    buildProperty: p => p.Name = "Is" + member.Name );
        }
    }

    // Template for the non-flags enum member.
    [Template]
    public bool IsMemberTemplate => meta.This._value == ( (IField)meta.Tags["member"]! ).Value;

    // Template for a flag enum member.
    [Template]
    public bool IsFlagTemplate
    {
        get
        {
            var field = (IField)meta.Tags["member"]!;

            // Note that the next line does not work for the "zero" flag, but currently Metalama does not expose the constant value of the enum
            // member so we cannot test its value at compile time.
            return ( meta.This._value & field.Value ) == ( (IField)meta.Tags["member"]! ).Value;
        }
    }

    [Template]
    private object? FieldTemplate;

    [Template]
    public void ConstructorTemplate( [CompileTime] IField valueField )
    {
        valueField.Value = meta.Target.Parameters[0].Value;
    }
}

// <target>
[EnumViewModel]
public class TargetClass
{
    [Flags]
    public enum StringOptions
    {
        None,
        ToUpperCase = 1,
        RemoveSpace = 2,
        Trim = 4
    }
}