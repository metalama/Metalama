// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Formatters;
using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Patterns.Caching.Formatters;

namespace Metalama.Patterns.Caching.Aspects;

internal sealed class ImplementFormattableAspect : TypeAspect
{
    [Template( Accessibility = Accessibility.Protected )]
    private void FormatCacheKey( UnsafeStringBuilder stringBuilder, IFormatterRepository formatterRepository )
    {
        if ( meta.Target.Method.OverriddenMethod != null )
        {
            meta.Proceed();
        }
        else
        {
            stringBuilder.Append( meta.This.GetType().FullName );
        }

        var stringBuilderExpression = ExpressionFactory.Capture( stringBuilder );
        var formatterRepositoryExpression = ExpressionFactory.Capture( formatterRepository );

        if ( formatterRepository.Role is CacheKeyFormatting )
        {
            var fieldOrProperties = meta.Target.Type.FieldsAndProperties.Where( p => p.Enhancements().HasAspect<CacheKeyAttribute>() ).OrderBy( b => b.Name );

            foreach ( var fieldOrProperty in fieldOrProperties )
            {
                // When [CacheKey] is applied to a backing field via [field: CacheKey], resolve
                // the corresponding property so that the generated code accesses the property instead
                // of the inaccessible compiler-generated backing field.
                var resolvedMember = fieldOrProperty;

                if ( fieldOrProperty is IField { IsImplicitlyDeclared: true } field && field.IsAutoPropertyBackingField() )
                {
                    var propertyName = field.Name.Substring( 1, field.Name.IndexOf( ">", StringComparison.Ordinal ) - 1 );

                    resolvedMember = meta.Target.Type.Properties.OfName( propertyName ).Single();
                }

                stringBuilder.Append( " " );

                meta.InvokeTemplate(
                    nameof(FormatFieldOrProperty),
                    args: new
                    {
                        T = resolvedMember.Type,
                        fieldOrProperty = resolvedMember,
                        stringBuilder = stringBuilderExpression,
                        formatterRepository = formatterRepositoryExpression
                    } );
            }
        }
    }

    [Template]
    private static void FormatFieldOrProperty<[CompileTime] T>(
        IFieldOrProperty fieldOrProperty,
        UnsafeStringBuilder stringBuilder,
        IFormatterRepository formatterRepository )
    {
        formatterRepository.Get<T>().Format( stringBuilder, fieldOrProperty.Value );
    }

    [UsedImplicitly]
    [InterfaceMember( IsExplicit = true, WhenExists = InterfaceMemberOverrideStrategy.Ignore )]
#pragma warning disable CA1822
    public void Format( UnsafeStringBuilder stringBuilder, IFormatterRepository formatterRepository )
    {
        meta.This.FormatCacheKey( stringBuilder, formatterRepository );
    }

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.ImplementInterface( typeof(IFormattable<CacheKeyFormatting>), whenExists: OverrideStrategy.Ignore );

        builder.IntroduceMethod(
            nameof(this.FormatCacheKey),
            whenExists: OverrideStrategy.Override,
            buildMethod:
            method => method.IsVirtual = !builder.Target.IsSealed );
    }
#pragma warning restore CA1822
}