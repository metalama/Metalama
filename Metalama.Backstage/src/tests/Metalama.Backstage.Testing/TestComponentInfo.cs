// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using System;

namespace Metalama.Backstage.Testing
{
    public class TestComponentInfo : IComponentInfo
    {
        public TestComponentInfo( string name, string? version, bool? isPrerelease, DateTime? buildDate, string? company )
        {
            this.Company = company;
            this.Name = name;
            this.PackageVersion = version;
            this.IsPrerelease = isPrerelease;
            this.BuildDate = buildDate;
        }

        public string? Company { get; }

        public string Name { get; }

        public string? PackageVersion { get; }

        public Version? AssemblyVersion => TestVersionHelper.GetAssemblyVersionFromPackageVersion( this.PackageVersion );

        public bool? IsPrerelease { get; }

        public DateTime? BuildDate { get; }
    }
}