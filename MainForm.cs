using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScriptRunner
{
    public partial class MainForm : Form
    {    
        public Config Config { get; set; }

        public List<System.Windows.Forms.Control> InputControls { get; set; }

        public MainForm(string[] args)
        {
            InitializeComponent();
            LoadConfig(args.Length > 0 ? args[0] : string.Empty);
            ApplyFormSettings();
            PopulateControls();
            LoadScripts();
        }

        private void ApplyFormSettings()
        {
            Text = Properties.Settings.Default.FORM_TITLE;

            if (File.Exists(Properties.Settings.Default.LOGO_PATH))
            {
                splitContainer2.Panel1Collapsed = false;
                pictureBox_Logo.Image = Image.FromFile(Properties.Settings.Default.LOGO_PATH);
            }

            splitContainer1.Orientation = Properties.Settings.Default.SPLITTER_ORIENTATION;

            Size = (splitContainer1.Orientation == Orientation.Vertical) ? new Size(600, 400) : new Size(300, 450);
        }

        private void LoadConfig(string path)
        {
            Config = GetConfig(string.IsNullOrEmpty(path) ? ".\\Config.json" : path);            
        }

        private void PopulateControls()
        {
            InputControls = new List<System.Windows.Forms.Control>();

            foreach (var control in Config.Controls.Reverse())
            {
                System.Windows.Forms.Control formControl = null;

                var label = new Label()
                {
                    Text = control.Label,
                    Dock = DockStyle.Top,
                    Font = new Font(FontFamily.GenericSansSerif, 10f, FontStyle.Regular),
                    Margin = new Padding(0, 5, 0, -1),
                };

                switch (control.Type)
                {
                    case "ComboBox":
                        formControl = new ComboBox()
                        {
                            DropDownStyle = ComboBoxStyle.DropDownList,                           
                        };
                        ((ComboBox)formControl).Items.AddRange(control.Values);
                        ((ComboBox)formControl).SelectedValueChanged += Input_Changed;
                        if (control.GetDefaultType() == typeof(long))
                        {
                            ((ComboBox)formControl).SelectedIndex = Convert.ToInt32((long)control.Default);
                        }
                        break;

                    case "TextBox":
                        formControl = new TextBox();                       
                        formControl.TextChanged += Input_Changed;
                        if (control.GetDefaultType() == typeof(string))
                        {
                            formControl.Text = (string)control.Default;
                        }
                        break;
                         
                    case "NumericUpDown":
                        formControl = new NumericUpDown();
                        ((NumericUpDown)formControl).ValueChanged += Input_Changed;
                        if (control.GetDefaultType() == typeof(long))
                        {
                            ((NumericUpDown)formControl).Value = Convert.ToInt32((long)control.Default);
                        }
                        break;

                    default:
                        //TODO: throw new Exception();
                        break;
                }
                
                formControl.Dock = DockStyle.Top;
                formControl.Margin = new Padding(0, 0, 0, 5);
                formControl.Font = new Font(FontFamily.GenericSansSerif, 10f, FontStyle.Regular);
                formControl.TabIndex = control.TabIndex;
                formControl.Tag = new object[]
                {
                    control.Required,
                    control.Alias
                };

                splitContainer2.Panel2.Controls.Add(formControl);
                splitContainer2.Panel2.Controls.Add(label);

                InputControls.Add(formControl);
            }

            // The control order is flipped
            // Here we're selecting the top control
            InputControls.Last().Select();

            CheckOkButtonIsValid();
        }
          

        public async Task RunScripts()
        {
            EnableControls(false);

            var failedScript = string.Empty;
            var statusColIndex = 2;


            foreach (var script in Config.Scripts)
            {
                var item = listView_Scripts.Items[script.Index];
                item.SubItems[statusColIndex].Text = "-";
            }

            foreach (var script in Config.Scripts)
            {
                var item = listView_Scripts.Items[script.Index];
                listView_Scripts.EnsureVisible(script.Index);

                if (!item.Checked) 
                {
                    item.SubItems[statusColIndex].Text = "Skipped";
                    continue;
                }

                item.SubItems[statusColIndex].Text = "Running";

                var result = await script.Run(GetArguments());

                item.SubItems[statusColIndex].Text = result ? "Success" : "Failed";

                if (!result)
                {
                    failedScript = $"{script.Description} ({script.FileInfo.FullName})";
                    for (int i = script.Index + 1; i < Config.Scripts.Length; i++)
                    {
                        listView_Scripts.Items[i].SubItems[statusColIndex].Text = "Not Ran";
                        listView_Scripts.EnsureVisible(i);
                    }

                    break;
                }                   
            }
            
            EnableControls(true);

            if (string.IsNullOrEmpty(failedScript))
                MessageBox.Show("All scripts completed successfully","Scripts Finished", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show($"An error ocoured with: {failedScript}", "Scripts Finished",  MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        public void EnableControls(bool enabled)
        {
            InputControls.ForEach(c => c.Enabled = enabled);
            
            button_Ok.Enabled = enabled;
            button_Close.Enabled = enabled;
            listView_Scripts.Enabled = enabled;
        }

        public Dictionary<string, string> GetArguments()
        {
            var dictionary = new Dictionary<string, string>();

            foreach (var control in InputControls)            
                dictionary.Add(((object[])control.Tag)[1].ToString(), control.Text);
              
            return dictionary;
        }

        public string GetConfigJsonText(string path)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Config GetConfig(string configPath)
        {
            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(GetConfigJsonText(configPath));
            }
            catch (Exception)
            {
                throw;
            }           
        }

        public Script[] LoadScripts()
        {            
            var scripts = Config.Scripts;

            for (int i = 0; i < scripts.Length; i++)
            {
                var script = scripts[i];
                script.Index = i;

                if (!script.FileInfo.Exists)
                {
                    MessageBox.Show($"Script does not exist: {script.FileInfo.FullName}\n\nPlease check config", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EnableControls(false);
                    listView_Scripts.Items.Clear();
                    break;
                }

                var item = new ListViewItem((script.Index + 1).ToString());
                item.Checked = script.Enabled;
                item.SubItems.AddRange(new string[]
                {
                   script.ToString(), "-", 
                });
               

                listView_Scripts.Items.Add(item);
            }

            listView_Scripts.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Scripts.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            return scripts;
        }
     
        public bool ValidateInput()
        {
            return InputControls.Where(c => (bool)((object[])c.Tag)[0]).Count(c => string.IsNullOrEmpty(c.Text)) == 0;
        }
       
        public void CheckOkButtonIsValid()
        {
            button_Ok.Enabled = ValidateInput();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Utils.WriteLog("Process started");
        }

        private void Input_Changed(object sender, EventArgs e)
        {
            CheckOkButtonIsValid();
        }

        private async void button_Ok_Click(object sender, EventArgs e)
        {
           await RunScripts();
        }
     
        private void button_Close_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
