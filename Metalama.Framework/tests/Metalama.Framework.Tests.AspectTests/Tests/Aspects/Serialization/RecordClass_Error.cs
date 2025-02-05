using Metalama.Framework.Serialization;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.RecordClass_Error;

/*
 * The record class error.
 */

//<target>
public record class TargetClass(int Foo) : ICompileTimeSerializable
{
}