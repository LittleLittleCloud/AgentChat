using System;
using System.IO;
using System.Threading.Tasks;

namespace GroupChatExample.Helper
{
    public class Logger
    {
        private string outputDirectory;

        public Logger(string outputDirectory)
        {
            this.outputDirectory = outputDirectory;

            if (!System.IO.Directory.Exists(this.outputDirectory))
            {
                System.IO.Directory.CreateDirectory(this.outputDirectory);
            }
        }

        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public async Task LogToFile(string relativePath, string content)
        {
            var path = System.IO.Path.Combine(this.outputDirectory, relativePath);
            var directory = System.IO.Path.GetDirectoryName(path);
            if (directory is not null && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, content);
        }
    }
}
