using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Security.AccessControl;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

public static class IOHelper
{
    public static void SearchFiles(string directory, Action<string> action, string extension)
    {
        var files = Directory.GetFiles(directory).Where(w => w.EndsWith(extension));

        foreach (var fileName in files)
        {
            action?.Invoke(fileName);
        }

        var directories = Directory.GetDirectories(directory);

        foreach (var innerDirectory in directories)
        {
            SearchFiles(innerDirectory, action, extension);
        }
    }

    public static void SearchDirectories(string directory, Action<string> action)
    {
        var directories = Directory.GetDirectories(directory);

        foreach (var innerDirectory in directories)
        {
            action?.Invoke(innerDirectory);

            SearchDirectories(innerDirectory, action);
        }

        /* var innerDirectories = Directory.GetDirectories(directory);

        foreach (var innerDirectory in innerDirectories)
        {
        } */
    }
}