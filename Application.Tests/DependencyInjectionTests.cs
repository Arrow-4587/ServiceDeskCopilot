using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceDesk.Application;

namespace Application.Tests;

[TestFixture]
public class DependencyInjectionTests
{
    [Test]
    public void AddApplicationServices_ShouldRegisterServicesWithoutError()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();

        Assert.That(provider, Is.Not.Null);
    }
}
