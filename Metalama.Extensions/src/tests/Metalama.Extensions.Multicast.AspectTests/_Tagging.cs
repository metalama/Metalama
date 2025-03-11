// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Eligibility;

namespace Metalama.Extensions.Multicast.AspectTests
{
    [AttributeUsage( AttributeTargets.All, AllowMultiple = true )]
    internal class AddTagAttribute : MulticastAspect, IAspect<IMethod>, IAspect<IConstructor>, IAspect<IFieldOrProperty>, IAspect<IEvent>, IAspect<IParameter>
    {
        private readonly string _tag;

        public AddTagAttribute( string tag ) :
            base( MulticastTargets.All )
        {
            this._tag = tag;
        }

        private void Implement<T>( IAspectBuilder<T> builder )
            where T : class, IDeclaration
        {
            this.Implementation.BuildAspect(
                builder,
                b => b.Advice.IntroduceAttribute( b.Target, AttributeConstruction.Create( typeof(TagAttribute), new[] { this._tag } ) ) );
        }

        public void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            this.Implement( builder );
        }

        public void BuildEligibility( IEligibilityBuilder<IMethod> eligibility ) { }

        public void BuildAspect( IAspectBuilder<IConstructor> builder )
        {
            this.Implement( builder );
        }

        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            this.Implement( builder );
        }

        public void BuildEligibility( IEligibilityBuilder<IConstructor> eligibility ) { }

        public void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
        {
            this.Implement( builder );
        }

        public void BuildEligibility( IEligibilityBuilder<IFieldOrProperty> eligibility ) { }

        public void BuildAspect( IAspectBuilder<IEvent> builder )
        {
            this.Implement( builder );
        }

        public void BuildEligibility( IEligibilityBuilder<IEvent> eligibility ) { }

        public void BuildAspect( IAspectBuilder<IParameter> builder )
        {
            this.Implement( builder );
        }

        public void BuildEligibility( IEligibilityBuilder<IParameter> eligibility ) { }
    }

    [AttributeUsage( AttributeTargets.All, AllowMultiple = true )]
    [Inheritable]
    internal class AddTagInheritedAttribute : MulticastAspect, IAspect<IMethod>, IAspect<IConstructor>, IAspect<IFieldOrProperty>, IAspect<IEvent>
    {
        private readonly string _tag;

        public AddTagInheritedAttribute( string tag, bool multicastOnInheritance = false ) :
            base(
                MulticastTargets.Method | MulticastTargets.Parameter | MulticastTargets.Class | MulticastTargets.Struct
                | MulticastTargets.InstanceConstructor | MulticastTargets.StaticConstructor | MulticastTargets.Parameter
                | MulticastTargets.Property | MulticastTargets.Event,
                multicastOnInheritance )
        {
            this._tag = tag;
        }

        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            this.Implement( builder );
        }

        private void Implement<T>( IAspectBuilder<T> builder )
            where T : class, IDeclaration
        {
            this.Implementation.BuildAspect(
                builder,
                b => b.Advice.IntroduceAttribute( b.Target, AttributeConstruction.Create( typeof(TagAttribute), new[] { this._tag } ) ) );
        }

        public void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            this.Implement( builder );
        }

        public void BuildEligibility( IEligibilityBuilder<IMethod> eligibility ) { }

        public void BuildAspect( IAspectBuilder<IConstructor> builder )
        {
            this.Implement( builder );
        }

        public void BuildEligibility( IEligibilityBuilder<IConstructor> eligibility ) { }

        public void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
        {
            this.Implement( builder );
        }

        public void BuildEligibility( IEligibilityBuilder<IFieldOrProperty> eligibility ) { }

        public void BuildAspect( IAspectBuilder<IEvent> builder )
        {
            this.Implement( builder );
        }

        public void BuildEligibility( IEligibilityBuilder<IEvent> eligibility ) { }
    }

    [AttributeUsage( AttributeTargets.All, AllowMultiple = true )]
    public class TagAttribute : Attribute
    {
        public TagAttribute( string tag ) { }
    }
}