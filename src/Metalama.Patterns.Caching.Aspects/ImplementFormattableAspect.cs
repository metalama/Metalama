// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters;
using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Patterns.Caching.Formatters;

namespace Metalama.Patterns.Caching.Aspects;

internal sealed class ImplementFormattableAspect : TypeAspect
{
    [Template]
    protected void FormatCacheKey( UnsafeStringBuilder stringBuilder, IFormatterRepository formatterRepository )
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
                stringBuilder.Append( " " );

                meta.InvokeTemplate(
                    nameof(FormatFieldOrProperty),
                    args: new
                    {
                        T = fieldOrProperty.Type,
                        fieldOrProperty,
                        stringBuilder = stringBuilderExpression,
                        formatterRepository = formatterRepositoryExpression
                    } );
            }
        }
    }

    [Template]
    private static void FormatFieldOrProperty<[CompileTime] T>(
        IFieldOrProperty fieldOrProperty,
        IExpression stringBuilder,
        IExpression formatterRepository )
    {
        ((IFormatterRepository) formatterRepository.Value!).Get<T>().Format( stringBuilder.Value, fieldOrProperty.Value );
    }

    [UsedImplicitly]
    [InterfaceMember( IsExplicit = true, WhenExists = InterfaceMemberOverrideStrategy.Ignore )]
    public void Format( UnsafeStringBuilder stringBuilder, IFormatterRepository formatterRepository )
    {
        this.FormatCacheKey( stringBuilder, formatterRepository );
    }

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.Advice.ImplementInterface( builder.Target, typeof(IFormattable<CacheKeyFormatting>), whenExists: OverrideStrategy.Ignore );

        builder.Advice.IntroduceMethod(
            builder.Target,
            nameof(this.FormatCacheKey),
            whenExists: OverrideStrategy.Override,
            buildMethod:
            method => method.IsVirtual = !builder.Target.IsSealed );
    }
}