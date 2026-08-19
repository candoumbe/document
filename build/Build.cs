using System;
using System.Collections.Generic;
using System.Linq;
using Candoumbe.Pipelines.Components;
using Candoumbe.Pipelines.Components.Formatting;
using Candoumbe.Pipelines.Components.GitHub;
using Candoumbe.Pipelines.Components.NuGet;
using Candoumbe.Pipelines.Components.Workflows;
using Documents.Aspire;
using Fallout.Common;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.Git;
using Fallout.Common.IO;
using Fallout.Common.ProjectModel;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.Codecov;
using Fallout.Common.Tools.Docker;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Tools.EntityFramework;
using Fallout.Common.Tools.GitHub;
using Fallout.Common.Tools.GitVersion;
using static Fallout.Common.Tools.Docker.DockerTasks;
using static Fallout.Common.Tools.DotNet.DotNetTasks;
using static Fallout.Common.Tools.EntityFramework.EntityFrameworkTasks;
using static Fallout.Common.Utilities.ConsoleUtility;
using static Serilog.Log;

[GitHubActions(
    "integration",
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = false,
    FetchDepth = 0,
    OnPushBranchesIgnore = [IHaveMainBranch.MainBranchName],
    PublishArtifacts = true,
    EnableGitHubToken = true,
    InvokedTargets = [nameof(Tests), nameof(PublishImages), nameof(IPack.Pack)],
    CacheKeyFiles = ["global.json", "src/**/*.csproj"],
    ImportSecrets =
    [
        nameof(IPushNugetPackages.NuGetApiKey),
        nameof(IReportCoverage.CodecovToken),
        nameof(IMutationTest.StrykerDashboardApiKey)
    ],
    OnPushExcludePaths =
    [
        ".github/ISSUE_TEMPLATE/*",
        "docs/*",
        "README.md",
        "CHANGELOG.md",
        "LICENSE"
    ]
)]
[GitHubActions(
    "delivery",
    GitHubActionsImage.UbuntuLatest,
    FetchDepth = 0,
    AutoGenerate = false,
    OnPushBranches = [ IHaveMainBranch.MainBranchName ],
    InvokedTargets = [nameof(Tests), nameof(PublishImages), nameof(ICreateGithubRelease.AddGithubRelease)],
    EnableGitHubToken = true,
    CacheKeyFiles = ["global.json", "src/**/*.csproj"],
    PublishArtifacts = true,
    ImportSecrets =
    [
        nameof(IPushNugetPackages.NuGetApiKey),
        nameof(IReportCoverage.CodecovToken),
        nameof(IMutationTest.StrykerDashboardApiKey)
    ],
    OnPushExcludePaths =
    [
        ".github/ISSUE_TEMPLATE/*",
        "docs/*",
        "README.md",
        "CHANGELOG.md",
        "LICENSE"
    ]
)]
[DotNetVerbosityMapping]
public class Build : EnhancedBuild,
    IHaveGitVersion,
    IHaveSourceDirectory,
    IHaveTestDirectory,
    IGitFlowWithPullRequest,
    IDoChoreWorkflow,
    IClean,
    IRestore,
    IDotnetFormat,
    // IMutationTest,
    IBenchmark,
    IReportUnitTestCoverage,
    IReportIntegrationTestCoverage,
    IPushNugetPackages,
    ICreateGithubRelease,
    ICanRegenerateGitHubWorkflows
{

    [Solution] [Required] public readonly Solution Solution;

    /// <inheritdoc />
    Solution IHaveSolution.Solution => Solution;

    public static int Main() => Execute<Build>(x => ((ICompile)x).Compile);

    ///<inheritdoc/>
    IEnumerable<AbsolutePath> IClean.DirectoriesToDelete =>
    [
        .. this.Get<IHaveSourceDirectory>().SourceDirectory.GlobDirectories("**/bin", "**/obj"),
        .. this.Get<IHaveTestDirectory>().TestDirectory.GlobDirectories("**/bin", "**/obj")
    ];

    ///<inheritdoc/>
    AbsolutePath IHaveSourceDirectory.SourceDirectory => RootDirectory / "src";

    ///<inheritdoc/>
    AbsolutePath IHaveTestDirectory.TestDirectory => RootDirectory / "tests";

    ///<inheritdoc/>
    IEnumerable<Project> IUnitTest.UnitTestsProjects => this.Get<IHaveSolution>().Solution.GetAllProjects("*.UnitTests");

    ///<inheritdoc/>
    IEnumerable<Project> IIntegrationTest.IntegrationTestsProjects => this.Get<IHaveSolution>().Solution.GetAllProjects("*.IntegrationTests");

    ///<inheritdoc/>
    Configure<DotNetTestSettings, (Project project, string framework)> IUnitTest.ProjectUnitTestSettings => (settings, unitTestRunContext) => settings
        .ResetProjectFile()
        .ClearLoggers()
        .SetProcessAdditionalArguments($"--project {unitTestRunContext.project.Path}");

    ///<inheritdoc/>
    Configure<DotNetTestSettings, (Project project, string framework)> IIntegrationTest.ProjectIntegrationTestSettings => (settings, testRunContext) => settings
        .ResetProjectFile()
        .ClearLoggers()
        .SetProcessAdditionalArguments($"--project {testRunContext.project.Path}");



    ///<inheritdoc/>
    IEnumerable<Project> IBenchmark.BenchmarkProjects => this.Get<IHaveSolution>().Solution.GetAllProjects("*.PerformanceTests");

    ///<inheritdoc/>
    bool IReportCoverage.ReportToCodeCov => this.Get<IReportCoverage>().CodecovToken is not null;

    ///<inheritdoc/>
    IEnumerable<AbsolutePath> IPack.PackableProjects => this.Get<IHaveSourceDirectory>().SourceDirectory
        .GlobFiles("**/*.csproj", "!**/*.API.csproj");

    ///<inheritdoc/>
    IEnumerable<PushNugetPackageConfiguration> IPushNugetPackages.PublishConfigurations =>
    [
        new GitHubPushNugetConfiguration(githubToken: this.Get<IHaveGitHubRepository>().GitHubToken,
                                         source: new Uri($"https://nuget.pkg.github.com/{this.Get<IHaveGitHubRepository>().GitRepository.GetGitHubOwner()}/index.json"),
                                         canBeUsed:() => this.Get<ICreateGithubRelease>()?.GitHubToken is not null)
    ];

    /// <inheritdoc />
    Configure<CodecovSettings> IReportIntegrationTestCoverage.CodecovSettings => _ => _.SetFlags("integration-tests");

    /// <inheritdoc />
    Configure<CodecovSettings> IReportUnitTestCoverage.CodecovSettings => _ => _.SetFlags("unit-tests");

    /// <inheritdoc />
    string IReportIntegrationTestCoverage.CodeCoverageReportArtifactName => "integration-test-coverage-report";

    /// <inheritdoc />
    string IReportIntegrationTestCoverage.CodeCoverageHistoryReportArtifactName => "integration-test-coverage-history-report";

    /// <inheritdoc />
    string IReportUnitTestCoverage.CodeCoverageReportArtifactName => "unit-test-coverage-report";

    /// <inheritdoc />
    string IReportUnitTestCoverage.CodeCoverageHistoryReportArtifactName => "unit-test-coverage-history-report";

    /// <inheritdoc />
    protected override void OnBuildCreated()
    {
        if (IsServerBuild)
        {
            EnvironmentInfo.SetVariable("DOTNET_ROLL_FORWARD", "LatestMajor");
        }
    }

    /// <inheritdoc/>
    bool IDotnetFormat.VerifyNoChanges => IsLocalBuild;

    /// <inheritdoc />
    Configure<DotNetFormatSettings> IDotnetFormat.FormatSettings => _ => _
                                                                        .When(_ => IsLocalBuild,
                                                                            settings => settings.SetVerbosity(DotNetVerbosity.diagnostic));

    private IReadOnlyList<Project> ArchitecturalTestsProjects => [.. this.Get<IHaveSolution>().Solution.AllProjects.Where(project => project.Name.Like("*.ArchitecturalTests", ignoreCase: true))];

    /// <summary>
    /// Target to run architectural tests.
    /// </summary>
    public Target ArchitecturalTests => _ => _.TryTriggeredBy<IUnitTest>() // <- This will make architectural tests run whenever unit tests run
                                            .TryBefore<IMutationTest>()
                                            .TryDependsOn<ICompile>()
                                            .Description("Runs architectural tests")
                                            .Executes(() =>
                                                          DotNetTest(s => s.SetConfiguration(this.Get<IHaveConfiguration>().Configuration)
                                                                         .SetNoBuild(SucceededTargets.Contains(this.Get<ICompile>().Compile))
                                                                         .SetNoRestore(SucceededTargets.Contains(this.Get<IRestore>().Restore))
                                                                         .CombineWith(ArchitecturalTestsProjects,
                                                                                      (setting, project) => setting.SetProcessAdditionalArguments($"--project {project.Path}")
                                                                                          .CombineWith(project.GetTargetFrameworks(),
                                                                                                       (x, framework) => x.SetFramework(framework)))
                                                                    )
                                                     );

    public Target AddMigration => _ => _.Description("Add a new migration to the database")
        .OnlyWhenStatic(() => IsLocalBuild)
        .Executes(() =>
        {
            string migrationName = PromptForInput("New migration name (leave empty to cancel the operation): ", string.Empty);
            if (string.IsNullOrWhiteSpace(migrationName))
            {
                return;
            }
            string provider = PromptForChoice("Database provider : ", [ ("Postgres",  "Postgres database engine" ), ("Sqlite", "SQLite database engine")]);
            if (string.IsNullOrWhiteSpace(provider))
            {
                return;
            }

            const string migrationDirectoryName = "Migrations";
            const string contextName = "Documents.DataStores.DocumentsStore";


            if(PromptForChoice($"Adding migration '{migrationName}' for provider '{provider}'. Confirm ?",
                   [ (ConsoleKey.Y, "Confirm the operation"),
                       (ConsoleKey.N, "Cancel the operation")]) == ConsoleKey.N)
            {
                Information("Operation cancelled by the user.");
                return;
            }

            string connectionString = provider switch
            {
                "Postgres" => "Host=localhost;Port=5432;Database=documents;Username=postgres;Password=!",
                "Sqlite" => "Data Source=documents.db",
                _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported provider")
            };

            string containerName = $"documents-migrations-temp-db-{Guid.CreateVersion7()}";
            DockerRun(s => s.SetImage("postgres:17-alpine")
                .SetDetach(true)
                .SetName(containerName)
                .SetProcessEnvironmentVariables(new Dictionary<string, string>
                {
                    ["POSTGRES_USER"] = "postgres",
                    ["POSTGRES_PASSWORD"] = "!",
                    ["POSTGRES_DB"] = "documents"
                }));

            EntityFrameworkMigrationsAdd(s => s.SetStartupProject(ApiProject)
                .SetName(migrationName)
                .SetContext(contextName)
                .SetProject(this.Get<IHaveSourceDirectory>().SourceDirectory / $"Documents.DataStores.{provider}" / $"Documents.DataStores.{provider}.csproj")
                .SetStartupProject(ApiProject)
                .SetOutputDirectory(migrationDirectoryName)
                .SetProcessAdditionalArguments($"""
                                                -- --provider {provider.ToLowerInvariant()} --ConnectionStrings:Documents "{connectionString}"
                                                """));

            DockerContainerRm(s => s.SetContainers(containerName));

            Information("Migration '{MigrationName}' added successfully.", migrationName);


        });

    public Target RemoveMigration => _ => _.Description("Remove latest migration")
        .OnlyWhenStatic(() => IsLocalBuild)
        .Executes(() =>
        {
            string provider = PromptForChoice("Database provider : ", [ ("Postgres",  "Postgres database engine" ), ("Sqlite", "SQLite database engine")]);
            if (string.IsNullOrWhiteSpace(provider))
            {
                return;
            }

            const string contextName = "Documents.DataStores.DocumentsStore";

            if(PromptForChoice($"Removing latest migration for provider '{provider}'. Confirm ?",
                   [ (ConsoleKey.Y, "Confirm the operation"),
                       (ConsoleKey.N, "Cancel the operation")]) == ConsoleKey.N)
            {
                Information("Operation cancelled by the user.");
                return;
            }

            string connectionString = provider switch
            {
                "Postgres" => "Host=localhost;Port=5432;Database=documents;Username=postgres;Password=!",
                "Sqlite" => "Data Source=documents.db",
                _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported provider")
            };


            DockerRun(s => s.SetImage("postgres:17-alpine")
                .SetDetach(true)
                .SetName("documents-migrations-temp-db")
                .SetProcessEnvironmentVariables(new Dictionary<string, string>
                {
                    ["POSTGRES_USER"] = "postgres",
                    ["POSTGRES_PASSWORD"] = "!",
                    ["POSTGRES_DB"] = "documents"
                }));

            DockerContainerRm(s => s.SetContainers("documents-migrations-temp-db"));


            EntityFrameworkMigrationsRemove(s => s.SetStartupProject(ApiProject)
                .SetContext(contextName)
                .SetProject(this.Get<IHaveSourceDirectory>().SourceDirectory / $"Documents.DataStores.{provider}" / $"Documents.DataStores.{provider}.csproj")
                .SetStartupProject(ApiProject)
                .SetProcessAdditionalArguments($"""
                                                -- --provider {provider.ToLowerInvariant()} --ConnectionStrings:Documents "{connectionString}"
                                                """));

            Information("Latest migration removed successfully.");


        });


    /// <summary>
    /// Pre-pulls every container image declared in <see cref="ContainerImages"/> so the download
    /// cost is paid before the integration test fixture starts the AppHost. This keeps cold CI
    /// runs from exhausting the AppHost startup timeout while pulling Postgres/RabbitMQ/Keycloak.
    /// </summary>
    public Target PrePullImages => _ => _
        .Description("Pulls Docker images required by integration tests so the pull time does not count against the AppHost startup timeout.")
        .TryDependentFor<IIntegrationTest>()
        .Executes(() =>
        {
            string[] references = [.. ContainerImages.All.Values.Select(image => image.FullReference)];
            DockerPull(settings => settings
                           .CombineWith(references,
                                        (s, image) => s.SetName(image)),
                       degreeOfParallelism: references.Length,
                       completeOnFailure: false);
        });

    private AbsolutePath ApiProject => this.Get<IHaveSourceDirectory>().SourceDirectory / "Documents.API";

    internal IReadOnlyList<RegistryConfiguration> Registries =>
    [
        new RegistryConfiguration("GitHub Container Registry",
                                  "ghcr.io",
                                  this.Get<IHaveGitHubRepository>().GitRepository.GetGitHubOwner(),
                                  this.Get<IHaveGitHubRepository>().GitHubToken)
    ];

    public Target PublishApi => _ => _.Description("Publish image of the API")
            .DependsOn<IPushNugetPackages>()
            .TriggeredBy<IPushNugetPackages>()
            .After(Tests)
            .TryAfter<IPack>()
            .Consumes(this.Get<ICompile>().Compile)
            .Produces(this.Get<IHaveArtifacts>().ArtifactsDirectory / "publish" / "**" / "*.tar.gz")
            .Executes(() =>
            {
                GitVersion gitVersion = this.Get<IHaveGitVersion>().GitVersion;
                string version = gitVersion.FullSemVer;
                const string imageName = "agenda.api";

                string filename = $"{imageName}-{version}.tar.gz";
                Project project = this.Get<IHaveSolution>().Solution.AllProjects.Single(project => project.Name == "Agenda.API");

                Registries.ForEach(registry =>
                {
                    AbsolutePath containerFullPath = this.Get<IHaveArtifacts>().ArtifactsDirectory / "publish"/ registry.Name / filename;

                    Information("Publishing {ImageName} (version {Version}) for {RegistryName} ({RegistryUri}) to {ContainerFullPath}",
                        project.Name, version, registry.Name, registry.Uri, containerFullPath);

                    string imageNameWithRegistry = $"{registry.Uri}/{this.Get<IHaveGitRepository>().GitRepository.GetGitHubOwner()}/{imageName}";
                    IDictionary<string, object> publishProperties = new Dictionary<string, object>
                    {
                        ["ContainerArchiveOutputPath"] = containerFullPath,
                        ["ContainerImageName"] = imageNameWithRegistry,
                        ["ContainerImageTag"] = gitVersion.SemVer,
                        ["ContainerGenerateLabelsImageCreated"] = DateTime.UtcNow.ToString("O")
                    };

                    DotNetPublish(settings => settings.SetProject(project)
                        .SetConfiguration(this.Get<IHaveConfiguration>().Configuration)
                        .EnableSelfContained()
                        .SetProperties(publishProperties)
                        .SetProcessAdditionalArguments([
                            "/t:PublishContainer",
                            "--tl"]));

                    Information("{ImageName} (version {Version} published successfully to {ContainerFullPath}", project.Name, version, containerFullPath);

                    Verbose("Loading image {ImageName} from {ContainerFullPath}", imageNameWithRegistry, containerFullPath);
                    DockerLoad(settings => settings.SetInput(containerFullPath));

                    Verbose("Image {ImageName} loaded successfully", imageNameWithRegistry);

                    IReadOnlySet<string> tags =  GenerateDockerTagsForBranch(this.Get<IHaveGitHubRepository>().GitRepository, gitVersion);
                    Verbose("Tagging image {ImageName} with tags: {Tags}", imageNameWithRegistry, string.Join(", ", tags));

                    DockerImageTag(settings => settings.SetSourceImage($"{imageNameWithRegistry}:{version}")
                        .CombineWith(tags, (dockerTagSettings, tag) => dockerTagSettings.SetTargetImage($"{imageNameWithRegistry}:{tag}")));

                    Verbose("Image {ImageName} tagged successfully", imageNameWithRegistry);

                    if (IsServerBuild)
                    {
                        Information("Pushing image {ImageName} to {RegistryName} ({RegistryUri}) with tags: {Tags}",
                            imageNameWithRegistry, registry.Name, registry.Uri, string.Join(", ", tags));

                        Verbose("Logging into {RegistryUri}", registry.Uri);

                        DockerLogin(settings => settings.SetUsername(this.Get<IHaveGitHubRepository>().GitRepository.GetGitHubOwner())
                            .SetPassword(registry.Password)
                            .SetServer(registry.Uri));

                        Verbose("Logged into {RegistryUri} successfully", registry.Uri);

                        DockerImagePush(settings =>
                            settings.CombineWith(tags, (pushSettings, tag) => pushSettings.SetName($"{imageNameWithRegistry}:{tag}")));

                        Information("Image {ImageName} pushed successfully", imageNameWithRegistry);
                    }
                });

            });

    public Target PublishWorker => _ => _.Description("Publish image of the migration worker")
        .After(Tests)
        .Consumes(this.Get<ICompile>().Compile)
        .Produces(this.Get<IHaveArtifacts>().ArtifactsDirectory / "publish" / "**" / "*.tar.gz")
        .Executes(() =>
        {
            GitVersion gitVersion = this.Get<IHaveGitVersion>().GitVersion;
            string version = gitVersion.FullSemVer;
            const string imageName = "document.worker";

            string filename = $"{imageName}-{version}.tar.gz";
            Project project = this.Get<IHaveSolution>().Solution.AllProjects.Single(project => project.Name == "Document.Migrator");

            Registries.ForEach(registry =>
            {
                AbsolutePath containerFullPath = this.Get<IHaveArtifacts>().ArtifactsDirectory / "publish" / registry.Name / filename;

                Information("Publishing {ImageName} (version {Version}) for {RegistryName} ({RegistryUri}) to {ContainerFullPath}",
                    project.Name, version, registry.Name, registry.Uri, containerFullPath);

                string imageNameWithRegistry = $"{registry.Uri}/{this.Get<IHaveGitRepository>().GitRepository.GetGitHubOwner()}/{imageName}";
                IDictionary<string, object> publishProperties = new Dictionary<string, object>
                {
                    ["ContainerArchiveOutputPath"] = containerFullPath,
                    ["ContainerRepository"] = imageNameWithRegistry,
                    ["ContainerImageTag"] = gitVersion.SemVer,
                    ["ContainerImageFormat"] = "Docker",
                    ["ContainerRuntime"] = "docker",
                    ["ContainerGenerateLabelsImageCreated"] = DateTime.UtcNow.ToString("O")
                };

                DotNetPublish(settings => settings.SetProject(project)
                    .SetConfiguration(this.Get<IHaveConfiguration>().Configuration)
                    .EnableSelfContained()
                    .SetProperties(publishProperties)
                    .SetProcessAdditionalArguments(["/t:PublishContainer", "--tl"]));

                Information("{ImageName} (version {Version} published successfully to {ContainerFullPath}", project.Name, version, containerFullPath);

                Verbose("Loading image {ImageName} from {ContainerFullPath}", imageNameWithRegistry, containerFullPath);
                DockerLoad(settings => settings.SetInput(containerFullPath));

                Verbose("Image {ImageName} loaded successfully", imageNameWithRegistry);

                IReadOnlySet<string> tags = GenerateDockerTagsForBranch(this.Get<IHaveGitHubRepository>().GitRepository, gitVersion);
                Verbose("Tagging image {ImageName} with tags: {@Tags}", imageNameWithRegistry, tags);

                DockerImageTag(settings => settings.SetSourceImage($"{imageNameWithRegistry}:{gitVersion.SemVer}")
                    .CombineWith(tags, (dockerTagSettings, tag) => dockerTagSettings.SetTargetImage($"{imageNameWithRegistry}:{tag}")));

                Verbose("Image {ImageName} tagged successfully", imageNameWithRegistry);

                if (IsServerBuild)
                {
                    Information("Pushing image {ImageName} to {RegistryName} ({RegistryUri}) with tags: {@Tags}",
                        imageNameWithRegistry, registry.Name, registry.Uri, tags);

                    Verbose("Logging into {RegistryUri}", registry.Uri);

                    DockerLogin(settings => settings.SetUsername(this.Get<IHaveGitHubRepository>().GitRepository.GetGitHubOwner())
                        .SetPassword(registry.Password)
                        .SetServer(registry.Uri));

                    Verbose("Logged into {RegistryUri} successfully", registry.Uri);

                    DockerImagePush(settings =>
                        settings.CombineWith(tags, (pushSettings, tag) => pushSettings.SetName($"{imageNameWithRegistry}:{tag}")));

                    Information("Image {ImageName} pushed successfully", imageNameWithRegistry);
                }
            });
        });

    public static IReadOnlySet<string> GenerateDockerTagsForBranch(GitRepository repository, GitVersion version)
    {
        HashSet<string> tags = new();

        if(repository.IsOnReleaseBranch())
        {
            tags.Add($"{version.Major}.{version.Minor}{version.PreReleaseLabelWithDash}");
            tags.Add($"{version.MajorMinorPatch}{version.PreReleaseLabelWithDash}");
        }
        else if (repository.IsOnHotfixBranch() || repository.IsOnFeatureBranch() || (repository.Branch?.StartsWith("chore/*", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            tags.Add(repository.Branch.Slugify());
        }
        else if (repository.IsOnDevelopBranch())
        {
            tags.Add($"{version.Major}-{version.EscapedBranchName}");
            tags.Add($"{version.Major}{version.PreReleaseLabelWithDash}");
            tags.Add($"{version.Major}.{version.Minor}{version.PreReleaseLabelWithDash}");
            tags.Add($"{version.Major}.{version.Minor}{version.EscapedBranchName}");
            tags.Add($"{version.MajorMinorPatch}{version.PreReleaseLabelWithDash}");
        }
        else if (repository.IsOnMainOrMasterBranch())
        {
            tags.Add($"{version.Major}");
            tags.Add($"{version.Major}-latest");
            tags.Add($"{version.Major}.{version.Minor}");
            tags.Add($"{version.Major}.{version.Minor}-latest");
            tags.Add($"{version.MajorMinorPatch}");
            tags.Add($"{version.MajorMinorPatch}-latest");
        }

        return tags;
    }
  
    public Target Tests => _ => _.Triggers(ArchitecturalTests,
                                           this.Get<IUnitTest>().UnitTests,
                                           this.Get<IIntegrationTest>().IntegrationTests)
                               .Description("Runs all tests");

        public Target PublishImages => _ => _.Description("Publish images of the API and frontend")
            .DependsOn(PublishApi, PublishWorker)
            .After(Tests)
            .TryBefore<ICreateGithubRelease>(x => x.AddGithubRelease)
            .Consumes(this.Get<ICompile>().Compile);

    // /// <summary>
    // /// Projects to be targeted by mutation tests.
    // /// </summary>
    // private static readonly string[] s_projects = ["Documents.Ids", "Documents.Objects", "Documents.API"];

    // /// <inheritdoc />
    // IEnumerable<MutationProjectConfiguration> IMutationTest.MutationTestsProjects =>
    // [
    //     ..s_projects.Select(projectName => new MutationProjectConfiguration(sourceProject: Solution.AllProjects.Single(csproj => csproj.Name == projectName),
    //                                                                        testProjects: Solution.AllProjects.Where(csproj => string.Equals(csproj.Name, $"{projectName}.UnitTests")),
    //                                                                        configurationFile: this.Get<IHaveTestDirectory>().TestDirectory / $"{projectName}.UnitTests" / "stryker-config.json"))
    // ];
}