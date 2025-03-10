// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Attributes;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.Transformations;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal static class IntroduceMemberTransformationFactory
{
    public static IInjectMemberTransformation CreateTransformation( this PropertyBuilder propertyBuilder, TemplateMember<IProperty>? template = null )
    {
        Invariant.Assert( propertyBuilder.OriginalField == null );

        return new IntroducePropertyTransformation( propertyBuilder.AspectLayerInstance, propertyBuilder.BuilderData, template );
    }

    public static ITransformation CreateTransformation( this AttributeBuilder attributeBuilder )
        => new IntroduceAttributeTransformation( attributeBuilder.AspectLayerInstance, attributeBuilder.BuilderData );

    public static IInjectMemberTransformation CreateTransformation( this ConstructorBuilder constructorBuilder )
        => constructorBuilder.IsStatic
            ? new IntroduceStaticConstructorTransformation( constructorBuilder.AspectLayerInstance, constructorBuilder.BuilderData )
            : new IntroduceConstructorTransformation( constructorBuilder.AspectLayerInstance, constructorBuilder.BuilderData );

    public static IInjectMemberTransformation CreateTransformation( this EventBuilder eventBuilder, TemplateMember<IEvent>? template = null )
        => new IntroduceEventTransformation( eventBuilder.AspectLayerInstance, eventBuilder.BuilderData, template );

    public static IInjectMemberTransformation CreateTransformation( this FieldBuilder fieldBuilder, TemplateMember<IField>? template = null )
        => new IntroduceFieldTransformation( fieldBuilder.AspectLayerInstance, fieldBuilder.BuilderData, template );

    public static IInjectMemberTransformation CreateTransformation( this IndexerBuilder indexerBuilder )
        => new IntroduceIndexerTransformation( indexerBuilder.AspectLayerInstance, indexerBuilder.BuilderData );

    public static IntroduceNamedTypeTransformation CreateTransformation( this NamedTypeBuilder namedTypeBuilder )
        => new( namedTypeBuilder.AspectLayerInstance, namedTypeBuilder.BuilderData );

    public static IntroduceNamespaceTransformation CreateTransformation( this NamespaceBuilder namespaceBuilder )
        => new( namespaceBuilder.AspectLayerInstance, namespaceBuilder.BuilderData );

    public static IInjectMemberTransformation ToTransformation( this MethodBuilder methodBuilder )
    {
        return new IntroduceMethodTransformation( methodBuilder.AspectLayerInstance, methodBuilder.BuilderData );
    }
}