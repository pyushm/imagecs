using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CodeEditor
{
    enum Conversion
    {
        None,
        Keep_Index_Brakets,
        Subscript_To_Index,
        Consts,
        Variables,
        Arrays,
        Equations_0D
    }
    enum ConversionState
    {
        None,
        ValueInput,
        SaveModel
    }
    public partial class ConvertorWindow : Window
    {
        StatementParser conversionParser = new StatementParser();
        CodeGenerator codeGenerator = new CodeGenerator();
        Button saveCodeBtn = new Button();
        TextBox defaultBox = new TextBox();
        Label defaultLabel = new Label();
        ConversionState state = ConversionState.None;
        bool userInput = false;
        string defaultSaveModelDir = @"C:\Users\pyushmanov\Documents\Projects\Git\ODEquationsSolver\bin\ODEquations";
        public ConvertorWindow()
        {
            InitializeComponent();
            defaultBox.Margin = new Thickness(2);
            defaultBox.Width = 350;
            defaultBox.Background = new SolidColorBrush(Color.FromRgb(0xEC, 0xe7, 0xe7) );
            defaultBox.BorderBrush = new SolidColorBrush(Colors.Black);
            defaultLabel.Content = "Default";
            saveCodeBtn.Margin = new Thickness(2);
            saveCodeBtn.Content = "  Save  ";
            saveCodeBtn.Background = new LinearGradientBrush(Colors.Lavender, Colors.LightGray, 90);
            DataContext = this; // set datacontext
            codeBox.ViewHelper = CodeViewHelper.CSharp3;
            codeBox.FontFamily = new FontFamily("Consolas");
            codeBox.FontSize = 14d;
            sourceBox.TextChanged += sourceTextChanged;
            conversionCombo.ItemsSource = Enum.GetNames(typeof(Conversion));
            conversionCombo.SelectionChanged += delegate (object sender, SelectionChangedEventArgs e) { conversionModeChanged(); };
            defaultBox.TextChanged += delegate (object sender, TextChangedEventArgs e) { if(userInput) createCodeText(); };
            saveCodeBtn.Click += delegate (object sender, RoutedEventArgs e) { saveModelCode(); };

            userInput = true;
            //List<uint> ul = new List<uint>();
            //ul.Add(1);
            //double rmin = 2;
            //int imin = 0;
            //int i = 1;
            //for (; ul.Count < 1000000; i++)
            //{
            //    uint sum = s2(ul);
            //    double r = (double)sum / i;
            //    if (rmin > r)
            //    {
            //        rmin = r;
            //        imin = i;
            //    }
            //    if (r == 1)
            //    {
            //        Console.WriteLine("solution: 2^" + i.ToString() + '=');
            //        Print(ul);
            //    }
            //    else if (r < 1)
            //    {
            //        Console.WriteLine("sum<N: 2^" + i.ToString() + '=');
            //        Print(ul);
            //    }
            //    if (i % 100000 == 0)
            //    {
            //        Console.WriteLine(i.ToString().PadLeft(8) + '\t' + imin.ToString().PadLeft(8) + '\t' + 
            //            rmin.ToString("f3") + '\t' + (r * i / ul.Count).ToString("f3") + '\t' + ul.Count);
            //        rmin = 2;
            //    }
            //}
            //Console.WriteLine("2^" + i.ToString() + '=');
            //Print(ul);
            //Application.Current.Shutdown();
        }
        //void Print(List<uint> cl)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    for (int i = 1; i <= cl.Count; i++)
        //    {
        //        sb.Append(cl[cl.Count - i]);
        //        if (i % 100 == 0)
        //        {
        //            Console.WriteLine(sb.ToString());
        //            sb.Clear();
        //        }
        //    }
        //    Console.WriteLine(sb.ToString());
        //}
        //uint s2(List<uint> cl)
        //{
        //    uint sum = 0;
        //    uint res = 0;
        //    for (int i = 0; i < cl.Count; i++)
        //    {
        //        uint j = cl[i];
        //        j *= 2;
        //        if (j > 9)
        //        {
        //            j += res - 10;
        //            res = 1;
        //        }
        //        else
        //        {
        //            j += res;
        //            res = 0;
        //        }
        //        sum += j;
        //        cl[i] = j;
        //    }
        //    if (res == 1)
        //    {
        //        cl.Add(1);
        //        sum += 1;
        //    }
        //    return sum;
        //}
        void Window_Loaded(object sender, RoutedEventArgs e) { }
        string createVariableDescription(List<Statement> results, string preffix, string suffix, string defaultValue)
        {
            StringBuilder decs = new StringBuilder();
            StringBuilder vals = new StringBuilder();
            StringBuilder warn = new StringBuilder();
            foreach (Statement res in results)
            { 
                string desc = res.Description.Trim();
                string c = res.LHS.Length == 0 ? "" : (desc.Length > 0 ? "[Description(\"" + desc + "\")]" + Environment.NewLine : "") + preffix + ' ' + res.LHS + ' ' + suffix;
                if (c.Length > 0)
                    decs.AppendLine(c);
                if (conversionParser.Errors != null && conversionParser.Errors.Length > 0)
                    warn.AppendLine(conversionParser.Errors);
                c = res.LHS.Length == 0 ? "" : res.IsAssignment ? res.Code + ';' : defaultValue.Length == 0 ? "" : res.LHS + " = " + defaultValue + ';';
                if (c.Length > 0)
                    vals.AppendLine(c);
                if (conversionParser.Errors != null && conversionParser.Errors.Length > 0)
                    warn.AppendLine(conversionParser.Errors);
            }
            string str = decs.ToString();
            if(vals.Length > 0)
                str += Environment.NewLine + vals.ToString();
            if(warn.Length > 0)
                str += " ERRORS:" + Environment.NewLine + warn.ToString();
            return str;
        }
        string convertToExpressions(List<Statement> results, bool replaceSubscript, bool replaceBrakets)
        {
            StringBuilder vars = new StringBuilder();
            StringBuilder warn = new StringBuilder();
            foreach (Statement res in results)
            {
                if (res == null)
                    return "";
                string c = replaceSubscript ? conversionParser.ReplaceSubscriptWithIndex(res) : res.Code;
                if (c.Length > 0)
                    c += ';';
                if (res.Description.Length > 0)
                    c += " // " + res.Description;
                if (c.Length > 0)
                    vars.AppendLine(c);
                if (conversionParser.Errors != null && conversionParser.Errors.Length > 0)
                    warn.AppendLine(conversionParser.Errors);
            }
            return warn.Length == 0 ? vars.ToString() : vars.ToString() + Environment.NewLine + " ERRORS:" + Environment.NewLine + warn.ToString();
        }
        void conversionModeChanged()
        {
            ConversionState prev = state;
            Conversion ct = (Conversion)Enum.Parse(typeof(Conversion), (string)conversionCombo.SelectedItem);
            state = ct == Conversion.Consts || ct == Conversion.Variables || ct == Conversion.Arrays ? ConversionState.ValueInput :
                ct == Conversion.Equations_0D ? ConversionState.SaveModel : ConversionState.None;
            //Debug.WriteLine(state.ToString());
            //foreach (UIElement e in convertionPanel.Children)
            //    Debug.WriteLine(e.GetType());
            if (prev != state)
            {
                if (prev == ConversionState.SaveModel || prev == ConversionState.ValueInput)
                {
                    int l = convertionPanel.Children.Count;
                    convertionPanel.Children.RemoveAt(l-1);
                    convertionPanel.Children.RemoveAt(l-2);
                }
                if (state == ConversionState.ValueInput)
                    convertionPanel.Children.Add(defaultLabel);
                else if (state == ConversionState.SaveModel)
                    convertionPanel.Children.Add(saveCodeBtn);
                if (state != ConversionState.None) 
                    convertionPanel.Children.Add(defaultBox);
            }
            createCodeText(true);
        }
        void createCodeText(bool updateDefaultValue = false) // called if any input changed
        {
            if (!userInput)
                return;
            Conversion ct = (Conversion)Enum.Parse(typeof(Conversion), (string)conversionCombo.SelectedItem);
            string preffix = ct == Conversion.Consts ? "readonly double" : ct == Conversion.Variables ? "public double" : ct == Conversion.Arrays ? "public double[]" : "";
            bool hasDefault = state == ConversionState.ValueInput;
            bool modelEq = state == ConversionState.SaveModel;
            string[] lines = conversionParser.ReadSource(sourceBox.Text);
            eqVarBox.Text = conversionParser.GetVariable("variable:", "t");
            string[] fa = conversionParser.GetVariables("functions:", functionBox.Text.Split(new char[] { ',' }));
            ConvertionSyntax convertion = new ConvertionSyntax(eqVarBox.Text, fa);
            List<Statement> statements = conversionParser.ProcessEquationLines(lines, modelEq, hasDefault || ct != Conversion.Keep_Index_Brakets, convertion);
            if (hasDefault)
            {
                if (updateDefaultValue)
                    defaultBox.Text = ct == Conversion.Variables ? "0" : ct == Conversion.Arrays ? "new double[N+1]" : "1";
                string suffix = ct == Conversion.Consts ? ";" : "{ get; private set; }";
                codeBox.Text = createVariableDescription(statements, preffix, suffix, defaultBox.Text);
            }
            else if (modelEq)
            {
                defaultBox.Text = "Model";
                codeBox.Text = codeGenerator.CreateCSharpCode(sourceBox.Text, statements, conversionParser, convertion.EquationVar);
                if (codeGenerator.Warnings.Length > 0)
                {
                    StringBuilder sb=new StringBuilder();
                    foreach (var s in codeGenerator.Warnings)
                        sb.AppendLine(s);
                    MessageBox.Show(sb.ToString());
                }
            }
            else
                codeBox.Text = convertToExpressions(statements, ct == Conversion.Subscript_To_Index, ct != Conversion.Keep_Index_Brakets);
        }
        void sourceTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!userInput)
                return;
            userInput = false;
            sourceBox.Text = MSWSyntax.CorrectEquationSource(sourceBox.Text); // converting to equations text from MSW source
            userInput = true;
            createCodeText();
        }
        void saveModelCode()
        {
            Microsoft.Win32.SaveFileDialog saveAsDialog = new Microsoft.Win32.SaveFileDialog();
            saveAsDialog.Filter = "Equations (*.cse)|*.cse";
            saveAsDialog.RestoreDirectory = true;
            saveAsDialog.InitialDirectory = defaultSaveModelDir;
            saveAsDialog.FileName = defaultBox.Text;
            if (saveAsDialog.ShowDialog() == true)
            {
                var nl = Environment.NewLine;
                string codeText = codeBox.Text + nl + "#region Equation source" + nl + "/* Equation source" + nl + sourceBox.Text + nl + "*/" + nl + "#endregion";
                string path = Path.Combine(defaultSaveModelDir, saveAsDialog.FileName);
                File.WriteAllText(path, codeText);
                userInput = false;
                int l = path.Length;
                if (l > 50)
                    path = "..."+path.Substring(l - 47);
                defaultBox.Text = "Saved to " +path;
                userInput = true;
            }
        }
    }
}
