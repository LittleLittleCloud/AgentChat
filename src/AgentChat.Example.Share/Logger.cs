using System;
using System.IO;
using System.Threading.Tasks;

namespace AgentChat.Example.Share;

public class Logger
{
    private readonly string outputDirectory;

    public Logger(string outputDirectory)
    {
        this.outputDirectory = outputDirectory;

        if (!Directory.Exists(this.outputDirectory))
        {
            Directory.CreateDirectory(this.outputDirectory);
        }
    }

    public void Log(string message)
    {
        Console.WriteLine(message);
    }

    public async Task LogToFile(string relativePath, string content)
    {
        var path = Path.Combine(outputDirectory, relativePath);
        var directory = Path.GetDirectoryName(path);

        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, content);
    }
}