using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Security.AccessControl;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

public class DirectoryHelper
{
    public static void SearchEmpty(string baseDirectory)
    {
        IOHelper.SearchDirectories(baseDirectory, SearchEmpty_Action);
    }

    private static void SearchEmpty_Action(string directory)
    {
        if (!Directory.GetFiles(directory).Any())
        {
            var metaFile = $"{directory}.meta";

            if (!File.Exists(metaFile))
                return;

            //Directory.Delete(directory);
            File.Delete($"{directory}.meta");

            System.Console.WriteLine($"Deleted: {directory}");
        }
    }
}