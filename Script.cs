using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;

namespace ScriptRunner
{

    public class _Scripts
    {
        public Script[] Scripts { get; set; }
    }

    public class Script
    {
        public int Index { get; set; }
        public string Description { get; set; }
        public FileInfo FileInfo { get; set; }
        public Dictionary<string,string> Arguments { get; set; }

        public Script(string scriptEntry, int index)
        {
            var parts = scriptEntry.Split(';');

            Description = parts[0];
            FileInfo = new FileInfo(parts[1]);            
            Index = index;
            Arguments = new Dictionary<string, string>();

            for (int i = 2; i < parts.Length; i++)
            {
                var subArgs = parts[i];
                var arg = new Argument(subArgs);
                Arguments.Add(arg.Param, arg.Value);
            }
        }

        public override string ToString()
        {
            return Description;
        }

        public string GetScriptText()
        {
            return File.ReadAllText(FileInfo.FullName);
        }
     
        public async Task<bool> Run(Dictionary<string,string> args)
        {            
            try
            {
                var keys = new List<string>(Arguments.Keys);

                using (Runspace runSpace = RunspaceFactory.CreateRunspace())
                {
                    runSpace.Open();

                    return await Task.Run(async () =>
                    {
                        using (Pipeline pipeline = runSpace.CreatePipeline())
                        {

                            Command script = new Command(GetScriptText(), true);
                            foreach (var key in keys)
                            {
                                var keyValue = Arguments[key];
                                var newValue = args[keyValue];
                                script.Parameters.Add(key, newValue);
                            }

                            pipeline.Commands.Add(script);
                            pipeline.Invoke();

                            return await Task.FromResult(!pipeline.HadErrors);
                        }
                    });                   
                }                                 
            }
            catch (Exception)
            {
                return await Task.FromResult(false);
            }
        }
    }

    public class Argument
    {
        public string Param { get; set; }
        public string Value { get; set; }

        public Argument(string argumentEntry)
        {
            var parts = argumentEntry.Split(' ');
            Param = parts[0].Replace("-",string.Empty);
            Value = parts[1];
        }
    }

}
