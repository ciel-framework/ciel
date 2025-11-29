using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Ciel;

public record ModuleDescriptor
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required Version Version { get; init; }
    public required List<string> Dependencies { get; init; } = new();
    public required string ProjectPath { get; init; }
    
    public string? Description { get; init; }
    public string? Copyright { get; init; }
    public string? ProjectUrl { get; init; }
    public string? LicenseUrl { get; init; }
    public string? Icon { get; init; }
    public string? RepositoryUrl { get; init; }
    public string? RepositoryType { get; init; }
    public string? Tags { get; init; }
    public string? ReleaseNotes { get; init; }
}

public class Module(ModuleDescriptor descriptor)
{
    public ModuleDescriptor Descriptor { get; } = descriptor;
    
    public string Id => Descriptor.Id;
    public string Title => Descriptor.Title;
    public Version Version => Descriptor.Version;
    public List<string> Dependencies => Descriptor.Dependencies;

    public override string ToString()
    {
        return $"{Descriptor.Title} {Descriptor.Version}";
    }
}

public class ModuleRegistry {
    
    public SortedDictionary<string, Module> Modules { get; init; }

    public static SortedDictionary<string, Module> Initialize(List<string> modulePaths)
    {
        SortedDictionary<string, Module> res = new();
        foreach (var solutionPath in modulePaths) {
            var solution = SolutionFile.Parse(solutionPath);
            foreach (var project in solution.ProjectsInOrder)
            {
                var projectRoot = ProjectRootElement.Open(project.AbsolutePath);
                
                var properties = projectRoot.Properties
                    .ToDictionary(p => p.Name, p => p.Value, StringComparer.OrdinalIgnoreCase);

                var moduleRefs = projectRoot.Items
                    .Where(item => item.ItemType == "ModuleReference")
                    .Select(item => item.Include)
                    .ToList();

                var descriptor = new ModuleDescriptor
                {
                    Id = properties["CielModule"],
                    Title = properties["Title"],
                    Version = new Version(properties["Version"]),
                    ProjectPath = project.AbsolutePath,

                    Description = properties.GetValueOrDefault("Description"),
                    Copyright = properties.GetValueOrDefault("Copyright"),
                    ProjectUrl = properties.GetValueOrDefault("PackageProjectUrl"),
                    LicenseUrl = properties.GetValueOrDefault("PackageLicenseUrl"),
                    Icon = properties.GetValueOrDefault("PackageIcon"),
                    RepositoryUrl = properties.GetValueOrDefault("RepositoryUrl"),
                    RepositoryType = properties.GetValueOrDefault("RepositoryType"),
                    Tags = properties.GetValueOrDefault("PackageTags"),
                    ReleaseNotes = properties.GetValueOrDefault("PackageReleaseNotes"),
                

                    Dependencies = moduleRefs
                };
                res.Add(descriptor.Id, new Module(descriptor));
            }
        }

        return res;
    }
}