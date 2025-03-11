// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Project;
using Metalama.Framework.Services;

namespace Metalama.Framework.Engine.CodeModel
{
    public sealed class ExecutionScenario : IExecutionScenario, IProjectService
    {
        public string Name { get; }

        public bool IsDesignTime { get; }

        public bool CapturesNonObservableTransformations { get; }

        public bool CapturesCodeFixImplementations { get; }

        public bool CapturesCodeFixTitles { get; }

        public bool IsTest { get; private set; }

        public static ExecutionScenario DesignTime { get; } = new( nameof(DesignTime), true, false, true, false );

        public static ExecutionScenario Preview { get; } = new( nameof(Preview), true, true, false, false );

        public static ExecutionScenario LiveTemplate { get; } = new( nameof(LiveTemplate), true, true, false, false );

        public static ExecutionScenario CompileTime { get; } = new( nameof(CompileTime), false, true, false, false );

        public static ExecutionScenario CodeFix { get; } = new( nameof(CodeFix), true, false, true, true );

        public static ExecutionScenario Introspection { get; } = new( nameof(Introspection), false, true, true, false );

        private ExecutionScenario(
            string name,
            bool isDesignTime,
            bool capturesNonObservableTransformations,
            bool capturesCodeFixTitles,
            bool capturesCodeFixImplementations )
        {
            this.Name = name;
            this.IsDesignTime = isDesignTime;
            this.CapturesNonObservableTransformations = capturesNonObservableTransformations;
            this.CapturesCodeFixImplementations = capturesCodeFixImplementations;
            this.CapturesCodeFixTitles = capturesCodeFixTitles;
        }

        // Resharper disable once UnusedMember.Global
        internal ExecutionScenario WithTest()
        {
            var clone = (ExecutionScenario) this.MemberwiseClone();
            clone.IsTest = true;

            return clone;
        }
    }
}