// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Carries flags that inform the linker whether call-site advice walkers need to run for the current
/// compilation. The struct is threaded from <see cref="AspectLinkerInput"/> through
/// <see cref="LinkerInjectionStepOutput"/> to <see cref="LinkerAnalysisStep"/>, where the flags are used
/// to short-circuit expensive syntax-tree walkers when they cannot possibly produce any rewrites.
/// New call-site advice kinds can be added here without touching the linker plumbing.
/// </summary>
/// <param name="ReferencesContainInitializableTypes">
/// Indicates whether any referenced assembly contains a type implementing
/// <see cref="Metalama.Framework.RunTime.Initialization.IInitializable"/>. Aggregated by
/// <c>TransitivePipelineContributorSource</c> from each referenced assembly's
/// <c>TransitiveAspectsManifest.ContainsInitializableTypes</c>. Used by <see cref="LinkerAnalysisStep"/>
/// to decide whether the on-initialized call-site walker needs to run.
/// </param>
internal readonly record struct CallSiteAdviceInfo( bool ReferencesContainInitializableTypes );
