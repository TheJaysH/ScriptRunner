using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;

namespace ScriptRunner
{

    public class Config
    {
        public Script[] Scripts { get; set; }
        public Control[] Controls { get; set; }
    }

    public class Control
    {
        public string Label { get; set; }
        public string Alias { get; set; }
        public string Type { get; set; }
        public string[] Values { get; set; }
        public bool Required { get; set; }
        public bool Locked { get; set; }
        public object Default { get; set; }
        public int TabIndex { get; set; }
        public Type GetDefaultType()
        {
            return Default?.GetType();
        }      
    }

    public class Script
    {
        public int Index { get; set; }
        public string Description { get; set; }
        public string FullPath { get; set; }
        public bool Enabled { get; set; }
        public Parameters[] Parameters { get; set; }

        public FileInfo FileInfo 
        { 
            get => new FileInfo(FullPath); 
        }

        public override string ToString()
        {
            return Description;
        }

        public string GetScriptText()
        {
            return File.ReadAllText(FileInfo?.FullName);
        }

        public async Task<bool> Run(Dictionary<string, string> args)
        {
            try
            {
                using (Runspace runSpace = RunspaceFactory.CreateRunspace())
                {
                    runSpace.Open();

                    return await Task.Run(async () =>
                    {
                        using (Pipeline pipeline = runSpace.CreatePipeline())
                        {

                            Command script = new Command(GetScriptText(), true);
                            foreach (var arg in Parameters)
                            {
                                script.Parameters.Add(arg.Key, args[arg.Value]);
                            }

                            pipeline.Commands.Add(script);

                            //pipeline.Output.DataReady += Output_DataReady;
                            //pipeline.Error.DataReady += Error_DataReady;        

                            var result = pipeline.Invoke();
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

        //private void Error_DataReady(object sender, EventArgs e)
        //{
        //    var output = (PipelineReader<PSObject>)sender;
        //    Debug.WriteLine(output);
        //}

        //private void Output_DataReady(object sender, EventArgs e)
        //{
        //    var output = (PipelineReader<PSObject>)sender;
        //    Debug.WriteLine(output);
        //}
    }

    public class Parameters
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
