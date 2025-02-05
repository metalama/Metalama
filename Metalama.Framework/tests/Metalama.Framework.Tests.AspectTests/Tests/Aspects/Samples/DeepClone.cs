#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER)
#endif

// In .NET Framework we get: Target runtime doesn't support covariant return types in overrides. Return type must be 'Targets.AutomaticallyCloneable'
// to match overridden member 'Targets.AutomaticallyCloneable.Clone()'`

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067
#pragma warning disable CS0169, CS8618, CS8602, CS8603

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Samples.DeepClone
{
    [Inheritable]
    internal class DeepCloneAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceMethod(
                nameof(CloneImpl),
                whenExists: OverrideStrategy.Override,
                args: new { T = builder.Target },
                buildMethod: m =>
                {
                    m.Name = "Clone";
                    m.ReturnType = builder.Target;
                } );

            builder.ImplementInterface(
                typeof(ICloneable),
                whenExists: OverrideStrategy.Ignore );
        }

        [Template( IsVirtual = true )]
        public T CloneImpl<[CompileTime] T>()
        {
            // This compile-time variable will receive the expression representing the base call.
            // If we have a public Clone method, we will use it (this is the chaining pattern). Otherwise,
            // we will call MemberwiseClone (this is the initialization of the pattern).
            IExpression baseCall;

            if (meta.Target.Method.IsOverride)
            {
                baseCall = meta.Base.Clone();
            }
            else
            {
                baseCall = meta.Base.MemberwiseClone();
            }

            // Define a local variable of the same type as the target type.
            var clone = (T)baseCall.Value!;

            // Select clonable fields.
            var clonableFields =
                meta.Target.Type.FieldsAndProperties.Where(
                    f => f.IsAutoPropertyOrField.GetValueOrDefault() &&
                         !f.IsImplicitlyDeclared &&
                         ( ( f.Type.IsConvertibleTo( typeof(ICloneable) ) && f.Type.SpecialType != SpecialType.String ) ||
                           ( f.Type is INamedType { BelongsToCurrentProject: true } fieldNamedType
                             && fieldNamedType.Enhancements().HasAspect<DeepCloneAttribute>() ) ) );

            foreach (var field in clonableFields)
            {
                // Check if we have a public method 'Clone()' for the type of the field.
                var fieldType = (INamedType)field.Type;
                var cloneMethod = fieldType.Methods.OfExactSignature( "Clone", Array.Empty<IType>() );

                if (cloneMethod is { Accessibility: Accessibility.Public } ||
                    fieldType.Enhancements().HasAspect<DeepCloneAttribute>())
                {
                    // If yes, call the method without a cast.
                    field.With( clone ).Value = meta.Cast( fieldType, field.Value?.Clone() );
                }
                else
                {
                    // If no, use the interface.
                    field.With( clone ).Value = meta.Cast( fieldType, ( (ICloneable?)field.Value )?.Clone() );
                }
            }

            return clone;
        }

        [InterfaceMember( IsExplicit = true )]
        private object Clone() => meta.This.Clone();
    }

    // <target>
    [DeepClone]
    internal partial class AutomaticallyCloneable
    {
        public int A;

        public ManuallyCloneable? B;

        public AutomaticallyCloneable? C;

        public NotCloneable? D;
    }

    // <target>
    internal class ManuallyCloneable : ICloneable
    {
        public int E;

        public object Clone()
        {
            return new ManuallyCloneable() { E = E };
        }
    }

    // <target>
    internal class NotCloneable
    {
        public int F;
    }

    // <target>
    internal partial class Derived : AutomaticallyCloneable
    {
        public ManuallyCloneable? G { get; private set; }
    }
}