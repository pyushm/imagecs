using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Animation;
using CodeEditor;
using System.Collections.Generic;
using ImageProcessor;

namespace ShaderEffects
{
    public partial class ShaderEditor : Window
    {
        TextBox selected;
        EditorWindow codeEditor;
        ParametricEffect se = null;
        Image img = new Image();
        IntSize imagePixelSize = new IntSize();
        EffectType effectType = EffectType.None;
        BitmapAccess bitmapData = null;
        string fileName ="";
        public ShaderEditor()
        {
            InitializeComponent();
            PreviewKeyDown += CaptureCtrlVC;
            effectSelectionBox.ItemsSource = Enum.GetNames(typeof(EffectType));
            effectSelectionBox.SelectedIndex = 0;
            host.Children.Add(img);
            host.Background = Brushes.Azure;
            host.MouseDown+=new MouseButtonEventHandler(host_MouseDown);
            //host.PreviewKeyDown += CaptureCtrlVC;
            Closing += delegate (object sender, System.ComponentModel.CancelEventArgs e) { CloseCodeEditForm(); };
            //parameterValue.LostFocus += new RoutedEventHandler(parameterValue_TextChanged);
            //parameters.SelectionChanged +=new SelectionChangedEventHandler(parameters_SelectionChanged);
            for (short mv = 2; mv < 6; mv++)
            {   // only 0 minor version exists
                bool s = RenderCapability.IsPixelShaderVersionSupported(mv, 0);
                bool ss = RenderCapability.IsPixelShaderVersionSupportedInSoftware(mv, 0);
                int i = RenderCapability.MaxPixelShaderInstructionSlots(mv, 0);
                Debug.WriteLine("V_" + mv + "_0" + " hardware=" + s + " Software=" + ss + " Slots=" + i);
            }
        }
        void ResizeImage(object sender, SizeChangedEventArgs e)
        {
            ColumnDefinitionCollection cdc = grid.ColumnDefinitions;
            double w = cdc[1].ActualWidth;
            double h = grid.ActualHeight;
            if (img != null && img.Source!=null)
                img.RenderTransform = InitialTransform(w, h, (BitmapImage)img.Source, 1);
        }
        public static readonly DependencyProperty MixProperty = DependencyProperty.Register("Mix", typeof(double), typeof(ShaderEditor));
        private void SelectImage(object sender, RoutedEventArgs e)
        {   // 
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Images|*.jpg;*.png;*.bmp;*.gif|All Files|*.*";
            if (ofd.ShowDialog(this) == true)
            {
                //ImageSource oldSource = img.Source;
                //Matrix om = img.RenderTransform.Value;
                img.Stretch = Stretch.None;
                img.HorizontalAlignment = HorizontalAlignment.Left;
                img.VerticalAlignment = VerticalAlignment.Top;
                fileName = ofd.FileName;
                BitmapImage bitmap = new BitmapImage(new Uri(fileName));
                imagePixelSize = new IntSize(bitmap.PixelWidth, bitmap.PixelHeight);
                img.Source = bitmap;
                bitmapData = new BitmapAccess(bitmap);
                //host.lo
                //var imageInfo = new ImageFileInfo(new FileInfo(ofd.FileName));
                //BitmapAccess ba = BitmapAccess.LoadImage(imageInfo.FSPath, imageInfo.IsEncrypted);
                ColumnDefinitionCollection cdc = grid.ColumnDefinitions;
                double w = cdc[1].ActualWidth;
                double h = grid.ActualHeight;
                img.RenderTransform = InitialTransform(w, h, bitmap, 1);
                img.Opacity = (double)GetValue(MixProperty);
                DoubleAnimation timeline = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(1000)));
                img.BeginAnimation(OpacityProperty, timeline);
                if (se != null)
                {
                    se.SetImageSize(imagePixelSize);
                    DependencyProperty dp = se.Parameter("Aspect");
                    if (dp != null)
                         se.SetValue(dp, (double)imagePixelSize.Width/ imagePixelSize.Height);
                    DisplayParameterList();
                }
            }
        }
        private void SaveImage(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveAsDialog = new SaveFileDialog();
            saveAsDialog.FileName = System.IO.Path.GetFileNameWithoutExtension(fileName);
            saveAsDialog.Filter = "jpg|*.jpg|png|*.png"; // safe format relies on this order
            saveAsDialog.FilterIndex = 1;
            saveAsDialog.RestoreDirectory = true;
            saveAsDialog.InitialDirectory = System.IO.Path.GetDirectoryName(fileName);
            if ((bool)saveAsDialog.ShowDialog())
            {
                try
                {
                    BitmapSource src = (BitmapSource)img.Source;
                    string saveName = saveAsDialog.FileName;
                    RenderTargetBitmap rtb = new RenderTargetBitmap((int)imagePixelSize.Width, (int)imagePixelSize.Height, src.DpiX, src.DpiY, PixelFormats.Default);
                    
                    Transform t=img.RenderTransform;
                    img.RenderTransform = Transform.Identity;
                    img.UpdateLayout();
                    rtb.Render(img);
                    string ext = System.IO.Path.GetExtension(saveName);
                    BitmapEncoder baseEncoder = ext==".png" ? (BitmapEncoder)new PngBitmapEncoder() : new JpegBitmapEncoder();
                    baseEncoder.Frames.Add(BitmapFrame.Create(rtb));
                    FileInfo fi = new FileInfo(saveName);
                    using (Stream stm = fi.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)) { baseEncoder.Save(stm); }
                    img.RenderTransform = t;
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
#endif
                }
            }
        }
        public Transform InitialTransform(double areaW, double areaH, BitmapImage bi, double maxScale)
		{
            double scale = Math.Min(Math.Min(areaW / bi.PixelWidth, areaH / bi.PixelHeight), maxScale);
            double scaleX = scale * bi.DpiX / 96;
            double scaleY = scale * bi.DpiY / 96;
            double offsetX=areaW / 2 - bi.PixelWidth * scale / 2;
            double offsetY=areaH / 2 - bi.PixelHeight * scale / 2;
            return new MatrixTransform(scaleX, 0, 0, scaleX, offsetX, offsetY);
		}
        void host_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (bitmapData == null)
                return;
            Point mp = e.GetPosition(img);
            Color c = bitmapData.GetColor(mp);
            if (selected !=null && se != null)
            {
                DependencyProperty dp = se.Parameter(selected.Name);
                if (dp != null && dp.PropertyType == typeof(Color))
                {
                    if (selected != null && selected.Name == dp.Name)
                    {
                        selected.Background = new SolidColorBrush(c);
                        se.SetValue(dp, c);
                        img.Effect = se;
                        selected = null;
                    }
                }
            }
        }
        void parameterTextChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                TextBox tb = sender as TextBox;
                if (tb == null)
                    return;
                DependencyProperty dp = se.Parameter(tb.Name);
                if (dp != null && dp.PropertyType == typeof(double))
                {
                    double v = double.Parse(tb.Text);
                    if (v != (double)se.GetValue(dp))
                    {
                        se.SetValue(dp, v);
                        //img.Effect = se; // not need to reset
                    }
                }
            }
            catch { }
        }
        void effectSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string res = "";
            se = null;
            effectType = (EffectType)Enum.Parse(typeof(EffectType), (string)effectSelectionBox.SelectedItem, true);
            if (effectType != EffectType.None)
            {
                try
                {
                    se = ParametricEffect.Create(effectType);
                    if (se == null)
                        res = "Effect " + (string)effectSelectionBox.SelectedItem + " NOT FOUND";
                    else if (imagePixelSize.Width > 0 && imagePixelSize.Height > 0)
                        se.SetImageSize(imagePixelSize);
                }
                catch (Exception ex)
                {
                    se = null;
                    res = "Can't creat: " + ex.Message;
                    if(ex.InnerException != null)
                        res+= Environment.NewLine+ex.InnerException.Message;
                }
                if (se != null)
                {
                    if (codeEditor != null)
                    {
                        codeEditor.Text = se.Compiler.SourceCode;
                        codeEditor.Title = se.Name;
                    }
                    DisplayParameterList();
                }
            }
            resultBox.Text = res;
            img.Effect = se;
        }
        void DisplayParameterList()
        {
            if (se != null)
            {
                parameterValues.Children.Clear();
                int np = se.Parameters.Length;
                string[] items = new string[np];
                for (int i = 0; i < np; i++)
                {
                    TextBox tb = new TextBox();
                    DependencyProperty dp = se.Parameters[i];
                    if (dp == null)
                        continue;
                    if (dp.PropertyType == typeof(double))
                    {
                        tb.Text = ((double)se.GetValue(dp)).ToString("f3");
                        tb.LostFocus += parameterTextChanged;
                    }
                    if (dp.PropertyType == typeof(Color))
                    {
                        tb.Background = new SolidColorBrush(((Color)se.GetValue(dp)));
                        tb.PreviewMouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e) { selected = sender as TextBox; };
                    }
                    tb.Name = items[i] = dp.Name;
                    items[i] += ' '+i.ToString();
                    tb.Margin = new Thickness(1.1);
                    parameterValues.Children.Add(tb);
                }
                parameters.ItemsSource = items;
            }
            else
                parameters.ItemsSource = new string[] { };
        }
        void ApplyClick(object sender, RoutedEventArgs e) { UpdateShader(); }
        void EditClick(object sender, RoutedEventArgs e)
        {
            if (codeEditor == null || !codeEditor.IsLoaded)
                codeEditor = new EditorWindow(CodeViewHelper.Shader);
            codeEditor.Text = se != null ? se.Compiler.SourceCode : "";
            codeEditor.Show();
            codeEditor.TextChanged += UpdateShader;
        }
        void CloseCodeEditForm() { if (codeEditor != null) codeEditor.Close(); }
        void UpdateShader()
        {
            if (se == null || codeEditor == null)
                resultBox.Text = "";
            else if (se.Compiler.SourceCode != codeEditor.Text)
            {
                try
                {
                    resultBox.Text = se.UpdateShader(codeEditor.Text);
                    if (resultBox.Text != "")
                    {
                        codeEditor.HilightErrors(ParseCompilerErrors(resultBox.Text));
                    }
                }
                catch (Exception ex) { resultBox.Text = ex.Message; }
            }
            img.Effect = resultBox.Text == "" ? se : null;
        }
        List<SegmentLine> ParseCompilerErrors(string errors)
        {
            List<SegmentLine> segments = new List<SegmentLine>();
            //Line(14, 35): error X3000: unrecognized identifier 'n'
            //Line(14, 38 - 39): error X3000: unrecognized identifier 'uu'
            string lineMarker = "Line";
            int index = errors.IndexOf(lineMarker, 0, StringComparison.CurrentCultureIgnoreCase);
            while (index != -1)
            {
                index += lineMarker.Length;
                int io = errors.IndexOf('(', index);
                if (io < 0)
                    break;
                index = io + 1;
                int ic = errors.IndexOf(',', index);
                if (ic < 0)
                    break;
                int line;
                if (!int.TryParse(errors.Substring(index, ic- index), out line))
                    break;
                index = ic+1;
                int start=-1, end;
                int id = errors.IndexOf('-', index);
                if (id > 0)
                {
                    if (!int.TryParse(errors.Substring(index, id - index), out start))
                        break;
                    index = id+1;
                }
                int ip = errors.IndexOf(')', index);
                if (ip < 0)
                    break;
                if (!int.TryParse(errors.Substring(index, ip - index), out end))
                    break;
                if (id < 0)
                    start = end;
                index = ip+1;
                segments.Add(new SegmentLine(line, start, end- start+1));
                index = errors.IndexOf(lineMarker, index, StringComparison.CurrentCultureIgnoreCase);
            }
            return segments;
        }
        void AddImageFromClipboard()
        {
            if (!System.Windows.Clipboard.ContainsData(System.Windows.DataFormats.Bitmap))
                return;
            BitmapAccess clipboard = new BitmapAccess(ClipboardBitmapAccess.GetImage());
            int transparencyEdge = Math.Min((int)Math.Sqrt(clipboard.Width + clipboard.Height) / 3, 6);
            int edge = -1;
            try { edge=int.Parse(edgeBox.Text); }
            catch { edge = -1; }
            if (edge >= 0)
                transparencyEdge = edge;
            BitmapLayer clip = new BitmapLayer("Clip", clipboard, transparencyEdge);
            img.Source = clip.Image.Source;
            clip.InitializeTransforms(Width, Height, -1, null);
            host.Children.Add(clip);
        }
        public void CaptureCtrlVC(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //Debug.WriteLine("d Modifier=" + Keyboard.Modifiers.ToString() + " key=" + e.Key.ToString() + " " + Keyboard.IsKeyDown(Key.LeftCtrl));
            if (e.Key == Key.V && Keyboard.IsKeyDown(Key.LeftCtrl))
                AddImageFromClipboard();
            //else if (e.Key == Key.C && Keyboard.IsKeyDown(Key.LeftCtrl))
            //    SetClipboardFromSelection();
        }
    }
}
