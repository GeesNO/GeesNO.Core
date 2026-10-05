namespace GeesNO.Sample.Cli;

/// <summary>
/// Process entry point for the non-normative GeesNO sample CLI.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs the sample CLI.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>The stable process exit code.</returns>
    public static Task<int> Main(string[] args)
    {
        return new CliApplication().RunAsync(args, Console.Out, Console.Error, CancellationToken.None);
    }
}
