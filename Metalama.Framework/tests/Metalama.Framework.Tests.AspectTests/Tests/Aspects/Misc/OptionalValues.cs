using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Misc.OptionalValues
{
    internal class OptionalValueTypeAttribute : TypeAspect
    {
        private static readonly DiagnosticDefinition<INamedType> _missingNestedTypeError = new(
            "OPT001",
            Severity.Error,
            "The [OptionalValueType] aspect requires '{0}' to contain a nested type named 'Optional'" );

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
            var optionalValuesProperty = builder.IntroduceProperty(
                    nameof(OptionalValues),
                    buildProperty: p =>
                    {
                        p.Type = nestedType;
                        p.InitializerExpression = ExpressionFactory.Parse( $"new {nestedType.Name}()" );
                    } )
                .Declaration;

            var optionalValueType = (INamedType)TypeFactory.GetType( typeof(OptionalValue<>) );

            // For all automatic properties of the target type.
            foreach (var property in builder.Target.Properties.Where( p => p.IsAutoPropertyOrField ?? false ))
            {
                // Add a property of the same name, but of type OptionalValue<T>, in the nested type.
                var optionalProperty = builder.With( nestedType )
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
                        tags: new { optionalProperty = optionalProperty } );
            }
        }

        [Template]
        public dynamic? OptionalValues { get; private set; }

        [Template]
        public dynamic? OptionalPropertyTemplate { get; set; }

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
    }

    public struct OptionalValue<T>
    {
        public bool IsSpecified { get; private set; }

        public T Value { get; }

        public OptionalValue( T value )
        {
            Value = value;
            IsSpecified = true;
        }
    }

    // <target>
    [OptionalValueType]
    internal class Account
    {
        public string? Name { get; set; }

        public Account? Parent { get; set; }

        // Currently Metalama cannot generate new classes, so we need to have
        // an empty class in the code.
        public class Optional { }
    }
}