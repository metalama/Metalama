// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.InterfaceImplementation;

internal sealed partial class ImplementInterfaceAdvice
{
    public sealed record ImplementationResult : IInterfaceImplementationResult, IAdviserInternal, IInterfaceImplementationAdviser
    {
        private readonly INamedType? _targetDeclaration;
        private readonly IAdviceFactory? _adviceFactory;

        public ImplementationResult(
            INamedType interfaceType,
            InterfaceImplementationOutcome outcome,
            INamedType? targetDeclaration = default,
            IAdviceFactoryImpl? originalAdviceFactory = null )
        {
            Invariant.Implies( targetDeclaration == null || originalAdviceFactory == null, outcome == InterfaceImplementationOutcome.Ignore );

            this.InterfaceType = interfaceType;
            this.Outcome = outcome;
            this._targetDeclaration = targetDeclaration;
            this._adviceFactory = originalAdviceFactory?.WithExplicitInterfaceImplementation( interfaceType );
        }

        public INamedType InterfaceType { get; }

        public InterfaceImplementationOutcome Outcome { get; }

        IInterfaceImplementationAdviser IInterfaceImplementationResult.ExplicitMembers => this;

        INamedType IInterfaceImplementationAdviser.Target
        {
            get
            {
                if ( this._targetDeclaration == null )
                {
                    throw new InvalidOperationException();
                }

                return this._targetDeclaration;
            }
        }

        IAdviceFactory IAdviserInternal.AdviceFactory
            => this._adviceFactory
               ?? throw new InvalidOperationException( $"Can't introduce explicit interface members for {this.InterfaceType}, because it was ignored." );
    }
}