#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER)
#endif

#pragma warning disable CS8618

using System.ComponentModel;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Request30355;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OptionalValueTypeAttribute), typeof(NotifyPropertyChangedAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Request30355;

[Inheritable]
internal class NotifyPropertyChangedAttribute : TypeAspect
{
    #region Events

    [InterfaceMember]
    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion

    #region Private Methods

    [Template]
    private dynamic OverridePropertySetter( dynamic value )
    {
        if (value != meta.Target.Property.Value)
        {
            meta.Proceed();
            OnPropertyChanged( meta.Target.Property.Name );
        }

        return value;
    }

    #endregion

    #region Protected Methods

    [Introduce( WhenExists = OverrideStrategy.Ignore )]
    protected void OnPropertyChanged( string name )
    {
        PropertyChanged?.Invoke( meta.This, new PropertyChangedEventArgs( name ) );
    }

    #endregion

    #region Public Methods

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.ImplementInterface( typeof(INotifyPropertyChanged) );

        foreach (var property in builder.Target.Properties.Where( p => !p.IsAbstract && p.Writeability == Writeability.All ))
        {
            builder.With( property ).OverrideAccessors( null, nameof(OverridePropertySetter) );
        }
    }

    #endregion
}

internal class OptionalValueTypeAttribute : TypeAspect
{
    #region Fields

    private static readonly DiagnosticDefinition<INamedType> _missingNestedTypeError = new(
        "OPT001",
        Severity.Error,
        "The [OptionalValueType] aspect requires '{0}' to contain a nested type named 'Optional'" );

    #endregion

    #region Public Methods

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Find the nested type.
        var nestedType = builder.Target.Types.OfName( "Optional" ).FirstOrDefault();

        if (nestedType == null)
        {
            builder.Diagnostics.Report( _missingNestedTypeError.WithArguments( builder.Target ), builder.Target );

            return;
        }

        // Introduce a property in the main type to store the Optional object.
        var optionalValuesProperty =
            builder.IntroduceProperty(
                nameof(OptionalValues),
                buildProperty: p =>
                {
                    p.Type = nestedType;
                    p.InitializerExpression = ExpressionFactory.Parse( $"new {nestedType.Name}()" );
                } );

        var optionalValueType = (INamedType)TypeFactory.GetType( typeof(OptionalValue<>) );

        // For all automatic properties of the target type.
        foreach (var property in builder.Target.Properties.Where( p => p.IsAutoPropertyOrField ?? false ))
        {
            // Add a property of the same name, but of type OptionalValue<T>, in the nested type.
            var builtProperty = builder.With( nestedType )
                .IntroduceProperty(
                    nameof(OptionalPropertyTemplate),
                    buildProperty: p =>
                    {
                        p.Name = property.Name;
                        p.Type = optionalValueType.WithTypeArguments( property.Type );
                    } )
                .Declaration;

            // Override the property in the target type so that it is forwarded to the nested type.
            builder.With( property )
                .Override(
                    nameof(OverridePropertyTemplate),
                    tags: new { optionalProperty = builtProperty } );
        }
    }

    #endregion

    #region Public Properties

    [Template]
    public dynamic? OptionalPropertyTemplate { get; set; }

    [Template]
    public dynamic? OptionalValues { get; set; }

    [Template]
    public dynamic? OverridePropertyTemplate
    {
        get
        {
            var optionalProperty = (IProperty)meta.Tags["optionalProperty"]!;

            return optionalProperty.With( (IExpression)meta.This.OptionalValues ).Value!.Value;
        }

        set
        {
            var optionalProperty = (IProperty)meta.Tags["optionalProperty"]!;
            var optionalValueBuilder = new ExpressionBuilder();
            optionalValueBuilder.AppendVerbatim( "new " );
            optionalValueBuilder.AppendTypeName( optionalProperty.Type );
            optionalValueBuilder.AppendVerbatim( "( value )" );
            optionalProperty.With( (IExpression)meta.This.OptionalValues ).Value = optionalValueBuilder.ToValue();
        }
    }

    #endregion
}

public struct OptionalValue<T>
{
    #region Constructors

    public OptionalValue( T value )
    {
        Value = value;
        IsSpecified = true;
    }

    #endregion

    #region Public Properties

    public bool IsSpecified { get; private set; }

    public T Value { get; }

    #endregion
}

// <target>
[NotifyPropertyChanged]
internal partial class Person
{
    #region Constructors

    public Person( string firstName, string lastName )
    {
        FirstName = firstName;
        LastName = lastName;
    }

    #endregion

    #region Public Properties

    public string FirstName { get; set; }

    public string FullName
    {
        get
        {
            return $"{FirstName} {LastName}";
        }
    }

    public string LastName { get; set; }

    #endregion
}

// <target>
[OptionalValueType]
public partial class Account
{
    #region Public Properties

    public string? Name { get; set; }

    public Account? Parent { get; set; }

    #endregion

    public partial class Optional { }
}