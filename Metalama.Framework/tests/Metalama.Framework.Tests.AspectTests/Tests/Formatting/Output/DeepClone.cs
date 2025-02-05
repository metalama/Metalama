#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER) - Return type covariance not supported in .NET Framework
#endif

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067, CS8618, CS8602, CS8603, CS0169

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.Output
{
    internal class DeepCloneAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var typedMethod = builder.Advice.IntroduceMethod(
                builder.Target,
                nameof(CloneImpl),
                whenExists: OverrideStrategy.Override,
                buildMethod: m =>
                {
                    m.Name = "Clone";
                    m.ReturnType = builder.Target;
                } );

            builder.Advice.ImplementInterface(
                builder.Target,
                typeof(ICloneable),
                whenExists: OverrideStrategy.Ignore );
        }

        [Template( IsVirtual = true )]
        public virtual dynamic CloneImpl()
        {
            IExpression baseCall;

            if (!meta.Target.Method.IsOverride)
            {
                baseCall = (IExpression)meta.Base.MemberwiseClone();
            }
            else
            {
                baseCall = (IExpression)meta.Target.Method.Invoke()!;
            }

            var clone = meta.Cast( meta.Target.Type.ToNonNullable(), baseCall )!;

            // Select clonable fields.
            var clonableFields =
                meta.Target.Type.FieldsAndProperties.Where(
                    f => f.IsAutoPropertyOrField.GetValueOrDefault() &&
                         ( f.Type.IsConvertibleTo( typeof(ICloneable) ) ||
                           ( f.Type is INamedType { BelongsToCurrentProject: true } fieldNamedType
                             && fieldNamedType.Enhancements().GetAspects<DeepCloneAttribute>().Any() ) ) );

            foreach (var field in clonableFields)
            {
                field.With( (IExpression)clone ).Value = meta.Cast( field.Type, ( (ICloneable)field.Value! ).Clone() );
            }

            return clone;
        }

        [InterfaceMember( IsExplicit = true )]
        private object Clone()
        {
            return meta.This.Clone();
        }
    }

    internal class ManuallyCloneable : ICloneable
    {
        public object Clone()
        {
            return new ManuallyCloneable();
        }
    }

    // <target>
    internal class Targets
    {
        [DeepClone]
        private class AutomaticallyCloneable
        {
            private int a;

            private ManuallyCloneable? b;

            private AutomaticallyCloneable? c;
        }

        [DeepClone]
        private class Derived : AutomaticallyCloneable
        {
            private string d;
        }
    }
}