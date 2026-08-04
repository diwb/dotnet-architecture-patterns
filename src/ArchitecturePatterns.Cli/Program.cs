using ArchitecturePatterns.Cli;

var result = await ArchToolApp.Execute(args, CancellationToken.None);
Console.WriteLine(result.Output);
return result.ExitCode;
