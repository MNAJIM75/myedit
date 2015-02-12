﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using PowerShell = System.Management.Automation.PowerShell;
using System.Management.Automation.Host;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Document;
using System.ComponentModel;

// https://github.com/icsharpcode/SharpDevelop/blob/master/src/AddIns/BackendBindings/FSharpBinding/Resources/FS-Mode.xshd
// https://github.com/icsharpcode/SharpDevelop/tree/master/src/Libraries/AvalonEdit/ICSharpCode.AvalonEdit/Highlighting/Resources

namespace MyEdit
{

    public class PageModel : INotifyPropertyChanged
    {
        private string tabCaption; 
        public string TabCaption 
        {
            get
            {
                return tabCaption;
            }
            set
            {
                tabCaption = value;
                NotifyPropertyChanged("TabCaption");
            }
        }

        private bool isSelected;
        public bool IsSelected 
        {
            get
            {
                return isSelected;
            }
            set
            {
                isSelected = value;
                NotifyPropertyChanged("IsSelected");
            }
        }

        public TextDocument Document { get; set; }
        public FrameworkElement TabContent { get; set; }
        public IHighlightingDefinition Syntax { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }


    public sealed class ExampleViewModel : BaseViewModel
    {
        /// <summary>
        /// lots of this stuff shouldnt be in view model of course :D
        /// </summary>
        private ICommand _executeItemCommand;
        private readonly ObservableCollection<string> _items;
        private Process process;
        private MyHost myHost;
        private Runspace myRunSpace;
        private PowerShell powershell;
        private App app;
        //private TextEditor editor;
        private IHighlightingDefinition haskellSyntax;


        public ExampleViewModel(App app)
        {
            this.app = app;

            _items = new ObservableCollection<string>();

            var executingAssembly = Assembly.GetExecutingAssembly();
            foreach (var assembly in executingAssembly.GetReferencedAssemblies())
            {
                _items.Add("Referenced assembly: " + assembly.FullName);
            }

            _items.Add(string.Empty);
            _items.Add(string.Empty);
            _items.Add("Type a line and press ENTER, it will be added to the output...");
            _items.Add(string.Empty);


            myHost = new MyHost(app, _items);
            myRunSpace = RunspaceFactory.CreateRunspace(myHost);
            myRunSpace.Open();
            powershell = PowerShell.Create();
            powershell.Runspace = myRunSpace;
            string script = @"ls c:\perso";
            powershell.AddScript(script);
            powershell.AddCommand("out-default");
            powershell.Invoke();


            // Check the flags and see if they were set propertly.


            _executeItemCommand = new RelayCommand<string>(AddItem, x => true);

            //editor = new TextEditor();
            using (XmlTextReader reader = new XmlTextReader(@"Syntax\FS-Mode.xshd"))
            {
                haskellSyntax = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }

            this.PageModels = new ObservableCollection<PageModel>();
        }

        public IEnumerable<string> Items
        {
            get { return _items; }
        }

        public ICommand ExecuteItemCommand
        {
            get
            {
                return _executeItemCommand;
            }

            set
            {
                SetPropertyAndNotify(ref _executeItemCommand, value, "ExecuteItemCommand");
            }
        }

        private void AddItem(string item)
        {
            Task.Run(() =>
            {
                powershell.AddScript(item);
                powershell.AddCommand("out-default");
                powershell.Invoke();
            });

        }

        public ObservableCollection<PageModel> PageModels { get; set; }

        internal void NewTab(string title, string path, string p)
        {
            var _doc = new ICSharpCode.AvalonEdit.Document.TextDocument(p);
            var editor = new TextEditor();
            editor.ContextMenu = new ContextMenu();
            editor.Document = _doc;
            editor.Document.FileName = path;
            editor.Document.TextChanged += Document_TextChanged;
            editor.SyntaxHighlighting = haskellSyntax;
            var page = new PageModel { TabContent = editor, TabCaption = title, Document = _doc, Syntax = haskellSyntax, IsSelected = true };
            this.PageModels.Add(page);
        }

        void Document_TextChanged(object sender, EventArgs e)
        {
            var page = PageModels
                .First(pm => sender == pm.Document);

            if (!page.TabCaption.EndsWith("*")) page.TabCaption += "*";
        }



        internal void SwitchContent(string title, string path, string doc)
        {
            NewTab(title, path, doc);
        }

        internal void Save()
        {
            var page = PageModels.First(pm => pm.IsSelected);
            (page.TabContent as TextEditor).Save(page.Document.FileName);
            page.TabCaption = page.TabCaption.Replace("*", "");
        }
    }
}
