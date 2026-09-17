using NUnit.Framework;

namespace Domain.Tests;

[TestFixture]
public class DependencyRulesTests
{
    [Test]
    public void Domain_ShouldNotDependOnExternalFrameworksOrInfrastructure()
    {
        var domainAssembly = typeof(ServiceDesk.Domain.ServiceDeskDomainAssemblyMarker).Assembly;
        var referencedAssemblies = domainAssembly.GetReferencedAssemblies();

        var forbiddenPrefixes = new[]
        {
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Azure",
            "Microsoft.SemanticKernel",
            "ModelContextProtocol"
        };

        foreach (var refAssembly in referencedAssemblies)
        {
            foreach (var forbidden in forbiddenPrefixes)
            {
                Assert.That(
                    refAssembly.Name, 
                    Does.Not.StartWith(forbidden), 
                    $"Domain project must not depend on external framework/sdk '{refAssembly.Name}'."
                );
            }
        }
    }
}
