using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShaderEffects
{
    /// <summary>
    /// Interaction logic for EffectControlWindow.xaml
    /// </summary>
    public partial class EffectControlWindow : Window
    {
        EffectType effectType;
        ControlType[] controls;
        static ControlType[] ControlList(EffectType effectType)
        {
            if (effectType == EffectType.ColorAdjustment)
                return new ControlType[] { ControlType.Brightness, ControlType.Saturation, ControlType.Hue, ControlType.Transparency };
            if (effectType == EffectType.ColorFilter)
                return new ControlType[] { ControlType.ColorFilter };
            if (effectType == EffectType.Edge)
                return new ControlType[] { ControlType.EdgeDetection };
            if (effectType == EffectType.ViewPoint)
                return new ControlType[] { ControlType.ViewPoint };
            return new ControlType[0];
        }
        public EffectControlWindow(EffectType effectType_)
        {
            InitializeComponent();
            effectType = effectType_;
            Title = effectType.ToString();
            controls=ControlList(effectType_);
            int row = 0;
            Height = 150 * controls.Length;
            root.ColumnDefinitions.Add(new ColumnDefinition());
            foreach (ControlType a in controls)
            {
                root.RowDefinitions.Add(new RowDefinition());
                root.Children.Add(new MultiIndicatorControl(a, row++));
            }
            SizeChanged += new SizeChangedEventHandler(ResizeChildren);
        }
        void ResizeChildren(object sender, SizeChangedEventArgs e)
        {
            foreach (MultiIndicatorControl uie in root.Children)
            {
                if (uie != null)
                    uie.ResetLayout();
            }
        }
    }
}
