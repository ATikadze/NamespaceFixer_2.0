using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Security.AccessControl;
using System.IO;
using System.Linq;

public class Program
{
    static void Main(string[] args)
    {
        var baseDirectory = "/Users/atikadze/Documents/Projects/Unity/idle-game-builder (volikos) copy/Assets/Plugins/ClickerBuilder/Scripts";

        //DirectoryHelper.SearchEmpty(baseDirectory);

        return;

        var removeDirectory = "/Users/atikadze/Documents/Projects/Unity/idle-game-builder (volikos) copy/Assets/Plugins/ClickerBuilder/Scripts";
        var baseNamespace = "ClickerBuilder";

        var namespaceFixer = new NamespaceFixer(baseDirectory, baseNamespace, removeDirectory);

        //namespaceFixer.Fix();
        //NamespaceFixer.RemoveDuplicateNamespaces(baseDirectory);
    }
}