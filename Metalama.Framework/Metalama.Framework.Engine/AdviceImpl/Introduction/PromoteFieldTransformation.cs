// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.Introductions.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Introduced;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Transformations;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class PromoteFieldTransformation : IntroducePropertyTransformation, IReplaceMemberTransformation
{
    public PropertyBuilder OverridingProperty { get; }

    private readonly IFullRef<IField> _replacedField;

    public static PromoteFieldTransformation Create(
        ProjectServiceProvider serviceProvider,
        IField replacedField,
        AspectLayerInstance aspectLayerInstance )
    {
        var replacedFieldImpl = (IFieldImpl) replacedField;

        var propertyBuilder = new PropertyBuilder(
            aspectLayerInstance,
            replacedField.DeclaringType,
            replacedField.Name,
            true,
            true,
            true,
            replacedField is { IsStatic: false, Writeability: Writeability.ConstructorOnly },
            false,
            replacedField.Writeability == Writeability.ConstructorOnly )
        {
            Type = replacedField.Type,
            Accessibility = replacedField.Accessibility,
            Writeability = replacedField.Writeability switch
            {
                Writeability.None => Writeability.None,
                Writeability.ConstructorOnly => Writeability.InitOnly, // Read-only fields are promoted to init-only properties.
                Writeability.All => Writeability.All,
                _ => throw new AssertionFailedException( $"Unexpected Writeability: {replacedField.Writeability}." )
            },
            IsStatic = replacedField.IsStatic,
            IsRequired = replacedField.IsRequired,
            IsNew = replacedField.IsNew,
            HasNewKeyword = replacedFieldImpl.HasNewKeyword.AssertNotNull(),
            IsDesignTimeObservableOverride = false,
            OriginalField = replacedField,
            InitializerExpression = replacedField.InitializerExpression,

            // Hack: we pull the initializer template. A nicer strategy would be to wrap the template inside the InitializerExpression.
            InitializerTemplate = (replacedField as IntroducedField)?.FieldBuilderData.InitializerTemplate?.As<IFieldOrProperty>()
        };

        propertyBuilder.GetMethod.AssertNotNull().Accessibility = replacedField.Accessibility;

        propertyBuilder.SetMethod.AssertNotNull().Accessibility =
            replacedField switch
            {
                { Writeability: Writeability.ConstructorOnly } => Accessibility.Private,
                _ => replacedField.Accessibility
            };

        if ( replacedField.Attributes.Count > 0 )
        {
            var classificationService = serviceProvider.Global.GetRequiredService<AttributeClassificationService>();

            foreach ( var attribute in replacedField.Attributes )
            {
                if ( classificationService.MustMoveFromFieldToProperty(
                        attribute.Type.GetSymbol().AssertSymbolNullNotImplemented( UnsupportedFeatures.IntroducedAttributeTypes ) ) )
                {
                    propertyBuilder.AddAttribute( attribute.ToAttributeConstruction() );
                }
                else
                {
                    propertyBuilder.AddFieldAttribute( attribute.ToAttributeConstruction() );
                }
            }
        }

        propertyBuilder.Freeze();

        return new PromoteFieldTransformation( aspectLayerInstance, replacedField, propertyBuilder );
    }

    private PromoteFieldTransformation( AspectLayerInstance aspectLayerInstance, IField replacedField, PropertyBuilder overridingProperty ) : base(
        aspectLayerInstance,
        overridingProperty.BuilderData,
        null )
    {
        this.OverridingProperty = overridingProperty;
        this._replacedField = replacedField.ToFullRef();
    }

    public override InsertPosition InsertPosition => this._replacedField.ToInsertPosition();

    IFullRef<IMember> IReplaceMemberTransformation.ReplacedMember => this._replacedField;

    public override IFullRef<IDeclaration> TargetDeclaration => this._replacedField;
}