// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CompileTime;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Advising
{
    internal static class TemplateMemberFactory
    {
        public static TemplateMember<T> Create<T>(
            T implementation,
            TemplateClassMember templateClassMember,
            TemplateProvider templateProvider,
            IAdviceAttribute adviceAttribute,
            IObjectReader tags,
            TemplateKind selectedKind,
            TemplateKind interpretedKind )
            where T : class, IMemberOrNamedType
            => new(
                (ISymbolRef<T>) implementation.ToRef().As<T>(),
                templateClassMember,
                templateProvider,
                adviceAttribute,
                tags,
                selectedKind,
                interpretedKind );

        public static TemplateMember<T> Create<T>(
            T implementation,
            TemplateClassMember templateClassMember,
            TemplateProvider templateProvider,
            IAdviceAttribute adviceAttribute,
            IObjectReader tags,
            TemplateKind selectedKind = TemplateKind.Default )
            where T : class, IMemberOrNamedType
            => new( (ISymbolRef<T>) implementation.ToRef().As<T>(), templateClassMember, templateProvider, adviceAttribute, tags, selectedKind );

        public static TemplateMember<T> Create<T>(
            ISymbol symbol,
            TemplateClassMember templateClassMember,
            TemplateProvider templateProvider,
            IAdviceAttribute adviceAttribute,
            RefFactory refFactory,
            IObjectReader tags,
            TemplateKind selectedKind = TemplateKind.Default )
            where T : class, IMemberOrNamedType
            => new( refFactory.FromSymbol<T>( symbol ), templateClassMember, templateProvider, adviceAttribute, tags, selectedKind );

        public static TemplateMember<T> Create<T>(
            T implementation,
            TemplateClassMember templateClassMember,
            TemplateProvider templateProvider,
            IObjectReader tags,
            TemplateKind selectedKind = TemplateKind.Default )
            where T : class, IMemberOrNamedType
            => new(
                (ISymbolRef<T>) implementation.ToRef().As<T>(),
                templateClassMember,
                templateProvider,
                (ITemplateAttribute) templateClassMember.Attribute.AssertNotNull(),
                tags,
                selectedKind );

        public static TemplateMember<T> Create<T>(
            ISymbol symbol,
            TemplateClassMember templateClassMember,
            TemplateProvider templateProvider,
            RefFactory refFactory,
            IObjectReader tags,
            TemplateKind selectedKind = TemplateKind.Default )
            where T : class, IMemberOrNamedType
            => new(
                refFactory.FromSymbol<T>( symbol ),
                templateClassMember,
                templateProvider,
                (ITemplateAttribute) templateClassMember.Attribute.AssertNotNull(),
                tags,
                selectedKind );
    }
}