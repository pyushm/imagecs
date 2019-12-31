using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Ink;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Input;
using CustomControls;
using Microsoft.Win32;
using System.Windows.Media.Animation;
using ShaderEffects;
using ImageWindows;

namespace ImageWindows
{
    public partial class EditWindow : Window
    {
        DrawingPanel canvas;
        Window effectWindow;
        ContextMenu selectMenu;

        public EditWindow()
        {
            InitializeComponent();
            editingModeBox.ItemsSource = Enum.GetNames(typeof(CanvasEditingMode));
            editingModeBox.SelectedIndex = 0;
            editingModeBox.SelectionChanged += new SelectionChangedEventHandler(editingModeBox_SelectionChanged);
            effectBox.ItemsSource = Enum.GetNames(typeof(EffectType));
            effectBox.SelectedIndex = 0;
            effectBox.SelectionChanged += new SelectionChangedEventHandler(effectBox_SelectionChanged);
            scaleBox.ItemsSource = Enum.GetNames(typeof(ImageScale));
            scaleBox.SelectedIndex = 0;
            scaleBox.SelectionChanged += new SelectionChangedEventHandler(scaleBox_SelectionChanged);
            layerList.Items.Add("Canvas");
            layerList.SelectedIndex = 0;
            selectMenu = new ContextMenu();
            MenuItem menuItemsr1 = new MenuItem();
            menuItemsr1.Name="Rigde_1";
            menuItemsr1.Header = "Rigde 1";
            menuItemsr1.Click += new RoutedEventHandler(delegate(object s, RoutedEventArgs e) { CreateRidge(0); });
            selectMenu.Items.Add(menuItemsr1);
            layerList.ContextMenu = selectMenu;
        }
        public void DrawNewImage(string name)
        {
            //Load(name, true);
        }
        void WindowLoaded(object sender, RoutedEventArgs e)
        {
            canvas = new DrawingPanel(grid1, 1, 0);
            //SizeChanged += new SizeChangedEventHandler(ResizeDrawingPanel);
            Closed += new EventHandler(CloseChildren);
        }
        void CloseChildren(object sender, EventArgs e) { if (effectWindow != null) effectWindow.Close(); }
        //void ColorAdjustmentChanged(object sender, EventArgs e)
        //{
        //    float[] v = brightnessControl.GetValues();
        //    double[] brightnessVals = new double[] { v[2], v[3] + 0.5, (v[3] - v[1]) / (v[2] - v[0]) + 1, (v[5] - v[3]) / (v[4] - v[2]) + 1 };
        //    v = saturationControl.GetValues();
        //    double[] saturationVals = new double[v.Length];
        //    for (int i = 0; i < v.Length; i++)
        //        saturationVals[i] = v[i];
        //    v = hueControl.GetValues();
        //    double[] hueVals = new double[v.Length];
        //    for (int i = 0; i < v.Length; i++)
        //        hueVals[i] = v[i];
        //    canvas.ApplyEffect(brightnessVals, saturationVals, hueVals, null);
        //}
        void AddLayers(object sender, RoutedEventArgs e) { OpenFile(false); }
        void OpenFile(bool replace)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Images|*.jpg;*.png;*.bmp;*.gif;*.tif;*.mli;*.drw|All Files|*.*";
            //if (ofd.ShowDialog(this) == true)
            //    Load(ofd.FileName, replace);
        }
        //void Load(string fileName, bool replace)
        //{
        //    scaleBox.SelectedItem = ImageScale.Fit.ToString();
        //    canvas.DisplayScale = ImageScale.Fit;
        //    canvas.Load(fileName, replace);
        //    canvas.EditingMode = CanvasEditingMode.None;
        //    editingModeBox.SelectedItem = canvas.EditingMode.ToString();
        //    //layerList.ItemsSource = canvas.LayerNames;
        //    layerList.Items.Clear();
        //    string[] ln=canvas.GetLayerNames();
        //    for (int i = 0; i < ln.Length; i++)
        //        //if(!canvas.Layers[i].Deleted)
        //        layerList.Items.Add(ln[i]);
        //    layerList.SelectedIndex = canvas.ActiveLayerIndex;
        //}
        //void ResizeDrawingPanel(object sender, SizeChangedEventArgs e)
        //{
        //    canvas.OnHostResized();
        //}
        void CreateRidge(int resolution)
        {
            object layer = layerList.SelectedItem;
            VisualLayer vl = canvas.GetVisualLayer((string)layer);
            if (vl==null || !vl.IsBitmap)
                return;
            //string name = activeLayer.Name + "R" + resolution;
            //renderer.Layers.Add(new RidgeLayer(name, ((ImageLayer)activeLayer).Image, activeLayer.Geometry.Clone(), resolution, new Pixel.Transform(brightnessControl.GetValues())));
            //UpdateLayerList(name);
        }
//        void saveAsButton_Click(object sender, RoutedEventArgs e)
//        {
//            string fileName=canvas.FileName;
//            SaveFileDialog saveAsDialog = new SaveFileDialog();
//            saveAsDialog.FileName = Path.GetFileNameWithoutExtension(fileName);
//            saveAsDialog.Filter = ImageWindows.DrawingPanel.FileExtensionFilter; // safe format relies on this order
//            saveAsDialog.FilterIndex = (int)ImageWindows.DrawingPanel.FileType(fileName);
//            saveAsDialog.RestoreDirectory = true;
//            saveAsDialog.InitialDirectory = Path.GetDirectoryName(fileName);
//            if ((bool)saveAsDialog.ShowDialog())
//            {
//                try
//                {
//                    string saveName = saveAsDialog.FileName;
//                    SaveType st = (SaveType)saveAsDialog.FilterIndex;
//                    canvas.Save(saveName, st);
//                }
//                catch (Exception ex)
//                {
//#if DEBUG
//                    Console.WriteLine(ex.Message);
//                    Console.WriteLine(ex.StackTrace);
//#endif
//                }
//            }
//        }
        void editingModeBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            canvas.EditingMode = (CanvasEditingMode)Enum.Parse(typeof(CanvasEditingMode), (string)editingModeBox.SelectedItem);
        }
        void effectBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            EffectType type= (EffectType)Enum.Parse(typeof(EffectType), (string)effectBox.SelectedItem);
            //canvas.EffectType = type;
            if (type != EffectType.None)
            {
                try
                {
                    if (effectWindow != null)
                        effectWindow.Close();
                    effectWindow = new EffectControlWindow(type);
                    effectWindow.Closed += new EventHandler(delegate(object s, EventArgs ea) { effectWindow = null; });
                    effectWindow.Show();
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
        void scaleBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            //canvas.DisplayScale = (ImageScale)Enum.Parse(typeof(ImageScale), (string)scaleBox.SelectedItem);
        }
        //void RotateLeft(object sender, RoutedEventArgs e) { if (canvas.ActiveLayer != null) canvas.ActiveLayer.Apply(RotateFlip.Left); }
        //void RotateRight(object sender, RoutedEventArgs e) { if (canvas.ActiveLayer != null) canvas.ActiveLayer.Apply(RotateFlip.Right); }
        //void FlipVertical(object sender, RoutedEventArgs e) { if (canvas.ActiveLayer != null) canvas.ActiveLayer.Apply(RotateFlip.Vertical); }
        //void FlipHorizontal(object sender, RoutedEventArgs e) { if (canvas.ActiveLayer != null) canvas.ActiveLayer.Apply(RotateFlip.Horisontal); }
        void RotateLeft(object sender, RoutedEventArgs e) { }
        void RotateRight(object sender, RoutedEventArgs e) { }
        void FlipVertical(object sender, RoutedEventArgs e) { }
        void FlipHorizontal(object sender, RoutedEventArgs e) { }
    }
}