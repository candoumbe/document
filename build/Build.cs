using Candoumbe.Pipelines.Components;
using Nuke.Common.ProjectModel;

class Build : EnhancedNukeBuild,
    IHaveSolution,
    IHaveSourceDirectory,
    IClean,
    IRestore,
    ICompile

{
    [Solution] public readonly Solution Solution;

    /// <inheritdoc />
    Solution IHaveSolution.Solution => Solution;

    public static int Main () => Execute<Build>(x => ((ICompile)x).Compile);
}