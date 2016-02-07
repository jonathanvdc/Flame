using Flame.Compiler;
using Flame.Front.Cli;
using Flame.Front.Options;
using Flame.Front.Plugs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Flame.Front.Preferences
{
    public static class PreferenceFile
    {
        public static ICompilerOptions ReadPreferences(string Path, IOptionParser<string> Parser, ICompilerLog Log)
        {
            if (!File.Exists(Path))
            {
                return new StringCompilerOptions();
            }

            try
            {
                using (var stream = new FileStream(Path, FileMode.Open, FileAccess.Read))
                using (var reader = new StreamReader(stream))
                {
                    string contents = reader.ReadToEnd();
                    try
                    {
                        var serializer = new JavaScriptSerializer();
                        var dict = serializer.Deserialize<Dictionary<string, string>>(contents);
                        return new StringCompilerOptions(dict, Parser);
                    }
                    catch (Exception ex)
                    {
						Log.LogError(new LogEntry("preferences error", "could not deserialize JSON preference file '" + Path + "'."));
                        Log.LogException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
				Log.LogError(new LogEntry("preferences error", "could not open preference file '" + Path + "'."));
                Log.LogException(ex);
            }

            return new StringCompilerOptions();
        }

        public static ICompilerOptions ReadPreferences(IOptionParser<string> Parser, ICompilerLog Log)
        {
            string path = FlameAssemblies.FlameAssemblyDirectory.Combine("prefs.json").AbsolutePath.Path;
            return ReadPreferences(path, Parser, Log);
        }
    }
}
