using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Security.AccessControl;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

public class NamespaceFixer
{
    private readonly string _baseDirectory, _baseNamespace, _removeDirectory;
    private const string _namespaceKeyword = "namespace", _usingKeyword = "using";
    private const string _regexPattern = @"(@?[a-z_A-Z]\w+(?:\.@?[a-z_A-Z]\w+)*)";
    //private static Regex _contentsRegex = new Regex($@"^namespace {_regexPattern}");
    private const bool _updateFiles = false;
    private static Regex _namespaceNameRegex = new Regex(_regexPattern);

    private readonly List<AssemblyModel> _assemblies;
    private readonly List<FileNamespaceModel> _fileNamespaces;

    public NamespaceFixer(string baseDirectory, string baseNamespace, string removeDirectory = null)
    {
        _baseDirectory = baseDirectory;
        _baseNamespace = baseNamespace;

        if (string.IsNullOrEmpty(removeDirectory))
        {
            removeDirectory = _baseDirectory;
        }

        _removeDirectory = removeDirectory;

        _assemblies = new List<AssemblyModel>();
        _fileNamespaces = new List<FileNamespaceModel>();
    }

    public void Fix()
    {
        IOHelper.SearchFiles(_baseDirectory, SearchAssemblies, ".asmdef");
        IOHelper.SearchFiles(_baseDirectory, FixNamespace, ".cs");
        IOHelper.SearchFiles(_baseDirectory, AddNamespaces, ".cs");
    }

    public static void RemoveDuplicateNamespaces(string baseDirectory)
    {
        IOHelper.SearchFiles(baseDirectory, RemoveDuplicateNamespaces_Action, ".cs");
    }

    private static void RemoveDuplicateNamespaces_Action(string filePath)
    {
        var contentLines = File.ReadAllLines(filePath).ToList();
        var usings = new Dictionary<int, string>();
        int? firstUsingIndex = null;

        for (int i = 0; i < contentLines.Count; i++)
        {
            if (contentLines[i].Trim().StartsWith(_usingKeyword))
            {
                if (!firstUsingIndex.HasValue)
                    firstUsingIndex = i;

                usings.Add(i, contentLines[i].Trim());
            }
            else if (contentLines[i].Trim().StartsWith(_namespaceKeyword))
            {
                break;
            }
        }

        if (!firstUsingIndex.HasValue)
            return;

        var newUsings = new List<string>();
        var distinctUsings = usings.DistinctBy(d => d.Value);

        //System.Console.WriteLine($"{filePath}");

        for (int i = 0; i < contentLines.Count; i++)
        {
            if (!usings.ContainsKey(i))
            {
                newUsings.Add(contentLines[i]);
            }
        }

        foreach (var item in distinctUsings)
        {
            newUsings.Insert(firstUsingIndex.Value, item.Value);
        }

        if (_updateFiles)
            File.WriteAllLines(filePath, newUsings);
    }

    private void SearchAssemblies(string filePath)
    {
        var assemblyName = filePath.Split('/').Last();
        var content = File.ReadAllText(filePath);
        var model = JsonSerializer.Deserialize<AssemblyJsonModel>(content);
        model.References = model.References.Select(s => s.Replace("GUID:", string.Empty).Trim()).ToArray();

        var metaFileContent = File.ReadAllLines($"{filePath}.meta");

        var id = string.Empty;

        foreach (var line in metaFileContent)
        {
            if (line.Trim().StartsWith("guid:"))
            {
                id = line.Replace("guid:", string.Empty).Trim();

                break;
            }
        }

        var baseDirectory = filePath.Replace(assemblyName, string.Empty);
        //baseDirectory = filePath.Remove(baseDirectory.Length - 1);

        var assemblyModel = new AssemblyModel()
        {
            Id = id,
            References = model.References.ToList(),
            BaseDirectory = baseDirectory
        };

        _assemblies.Add(assemblyModel);
        //System.Console.WriteLine($"{assemblyName} ({assemblyModel.Id}) - {assemblyModel.References.Count()}");
        //System.Console.WriteLine($"{assemblyName} - {assemblyModel.BaseDirectory}");
    }

    private void FixNamespace(string filePath)
    {
        var correctNamespace = string.Empty;

        correctNamespace = filePath.Replace(_removeDirectory, _baseNamespace);

        var split = correctNamespace.Split('/');
        split = split.Take(split.Length - 1).ToArray();

        var newSplits = new List<string>();

        foreach (var item in split)
        {
            newSplits.Add(string.Join('_', _namespaceNameRegex.Matches(item).Select(s => s.Value)));
        }

        correctNamespace = string.Join('.', newSplits);

        var contentLines = File.ReadAllLines(filePath).ToList();
        var oldNamespace = string.Empty;

        for (int i = 0; i < contentLines.Count; i++)
        {
            if (contentLines[i].Trim().StartsWith(_namespaceKeyword))
            {
                oldNamespace = contentLines[i].Trim().Replace(_namespaceKeyword, string.Empty);

                contentLines[i] = $"{_namespaceKeyword} {correctNamespace}";
            }
        }

        if (string.IsNullOrEmpty(oldNamespace))
        {
            System.Console.WriteLine($"No namespace in - {filePath}");
        }
        else
        {
            /*             var where = _assemblies.Where(s => filePath.Contains(s.BaseDirectory));

                        if (where.Count() > 1)
                        {
                            System.Console.WriteLine(filePath);
                            System.Console.WriteLine(string.Join("\n", where.Select(s => s.BaseDirectory)));
                        } */

            var assembly = _assemblies.SingleOrDefault(s => filePath.Contains(s.BaseDirectory));

            if (assembly == null)
            {
                System.Console.WriteLine("No assembly for file: " + filePath);
            }

            _fileNamespaces.Add(new FileNamespaceModel(filePath, oldNamespace, correctNamespace, assembly));
            //contentLines.RemoveAt(contentLines.Count - 1);

            if (_updateFiles)
                File.WriteAllLines(filePath, contentLines);
        }
    }

    private void AddNamespaces(string filePath)
    {
        var contentLines = File.ReadAllLines(filePath).ToList();
        var change = false;
        var currentFileNamespace = _fileNamespaces.SingleOrDefault(s => s.FilePath == filePath);

        for (int i = 0; i < contentLines.Count; i++)
        {
            if (contentLines[i].Trim().StartsWith(_usingKeyword))
            {
                var oldNamespace = contentLines[i].Trim().Replace(_usingKeyword, string.Empty).Replace(";", string.Empty);
                var newNamespaces = _fileNamespaces.Where(w => w.OldNamespace == oldNamespace && (currentFileNamespace.Assembly.Id == w.Assembly.Id || currentFileNamespace.Assembly.References.Contains(w.Assembly.Id))).DistinctBy(d => d.NewNamespace);
                //var newNamespaces = _fileNamespaces.Where(w => w.OldNamespace == oldNamespace && w.Assembly.References.Contains(currentFileNamespace.Assembly.Id)).DistinctBy(d => d.NewNamespace);

                if (newNamespaces.Any())
                {
                    contentLines[i] = string.Join("\n", newNamespaces.Select(s => $"{_usingKeyword} {s.NewNamespace};"));

                    change = true;
                }
            }
        }

        if (change)
        {
            //contentLines.RemoveAt(contentLines.Count - 1);

            if (_updateFiles)
                File.WriteAllLines(filePath, contentLines);
        }
    }

    private class FileNamespaceModel
    {
        public string FilePath { get; set; }

        public string OldNamespace { get; set; }

        public string NewNamespace { get; set; }

        public AssemblyModel Assembly { get; set; }

        public FileNamespaceModel(string filePath, string oldNamespace, string newNamespace, AssemblyModel assembly)
        {
            FilePath = filePath;
            OldNamespace = oldNamespace;
            NewNamespace = newNamespace;
            Assembly = assembly;
        }
    }

    private class AssemblyModel
    {
        public string Id { get; set; }

        public List<string> References { get; set; }

        public string BaseDirectory { get; set; }
    }

    private class AssemblyJsonModel
    {
        [JsonPropertyName("references")]
        public string[] References { get; set; }
    }
}