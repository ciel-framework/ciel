namespace Ciel.Birb.Tests;

public class Tests
{
    [Test]
    public void MatchPaths()
    {
        Assert.That(RouteSegments.PathMatch("/", "/") != null);
        Assert.That(RouteSegments.PathMatch("/", "/hello") == null);
        Assert.That(RouteSegments.PathMatch("/{path...}", "/") != null);
        Assert.That(RouteSegments.PathMatch("/{path...}", "/hello") != null);

        Assert.Pass();
    }
}