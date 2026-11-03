using AdventureWorks.Core;
using Xunit;

namespace AdventureWorks.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class ArchitectureTests
{
    // Chapter 2's rule: Core depends on nothing but .NET. This
    // reads the assemblies Core's compiled code uses; a package
    // reference whose types are never used does not appear.
    [Fact]
    public void CoreUsesNoWebOrDataTypes()
    {
        var used = typeof(Order).Assembly
            .GetReferencedAssemblies().Select(a => a.Name!);

        Assert.DoesNotContain(used, name =>
            name.StartsWith("Microsoft.AspNetCore") ||
            name.StartsWith("Microsoft.EntityFrameworkCore") ||
            name.StartsWith("AdventureWorks"));
    }
}
