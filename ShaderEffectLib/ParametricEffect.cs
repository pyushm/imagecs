using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.IO;
using System.Text;
using Common;
using ImageProcessor;
using System.Collections.Generic;

namespace ShaderEffects
{
    public enum EffectType
    {
        None,
        ColorAdjustment,    // bitmap color adjustment
        ColorFilter,
        Edge,
        ViewPoint,
        Morph,
        GradientContrast,
        ThresholdSmoothing
    }
    [Serializable]
    public class EffectData
    {
        EffectType type;
        double[] values;
        public EffectType Type { get { return type; } }
        public double[] Values { get { return values; } set { values = value; } }
        public EffectData(EffectType t) { type = t; }
    }
    public abstract class EffectInput
    {
        public string Name;
        protected double initial;
        public abstract Type Type { get; }
        public abstract object Default { get; }
        public EffectInput(string name) { Name = name; }
        public abstract EffectInput Copy(string newName);
    }
    public class ColorInput : EffectInput
    {
        public ColorInput(string name) : base(name) { }
        public override Type Type { get { return typeof(Color); } }
        public override object Default { get { return new Color(); } }
        public override EffectInput Copy(string newName) { return new ColorInput(newName); }
        public override string ToString() { return Name + " dflt=" + ((Color)Default).ToString(); }
    }
    public class FloatInput : EffectInput
    {
        public FloatInput(string name, double init) : base(name) { initial = init; }
        public override Type Type { get { return typeof(double); } }
        public override object Default { get { return initial; } }
        public override EffectInput Copy(string newName) { return new FloatInput(newName, initial); }
        public override string ToString() { return Name + " dflt=" + ((double)Default).ToString(); }
    }
    public class InputGroup : List<EffectInput>
    {
        // value transformation: v = NewLevel+(v-OldLevel)*(v>OldLevel ? BrightCoef : DarkCoef)
        static public InputGroup bilinear = new InputGroup() { new FloatInput("OldLevel", 0.5), new FloatInput("NewLevel", 0.5), new FloatInput("DarkCoef", 1), new FloatInput("BrightCoef", 1) };
        static public InputGroup saturations = new InputGroup() { new FloatInput("SatR", 1), new FloatInput("SatG", 1), new FloatInput("SatB", 1), new FloatInput("SatA", 1) };
        static public InputGroup lsh = new InputGroup() { new FloatInput("Lum", 1), new FloatInput("Sat", 1), new FloatInput("Hue", 1), new FloatInput("Opa", 1) };
        static public InputGroup delta = new InputGroup() { new FloatInput("DX", 0.001), new FloatInput("Aspect", 0) };// dx=DX*image.Width (in pixels); image.Width/image.Height
        static public InputGroup opacity = new InputGroup() { new FloatInput("opacity", 1), new FloatInput("opacitySlope", 0) };
        public InputGroup() { }
        public void AddGroup(InputGroup add) { foreach (var inp in add) AddUniqueInput(inp); }
        public void AddGroup(InputGroup add, string prefix) { foreach (var inp in add) AddUniqueInput(inp.Copy(prefix + inp.Name)); }
        public void AddFloat(string name, double init) { AddUniqueInput(new FloatInput(name, init)); }
        public void AddColor(string name) { AddUniqueInput(new ColorInput(name)); }
        void AddUniqueInput(EffectInput input)
        {
            foreach (var inp in this)
                if (input.Name == inp.Name)
                    throw new Exception("Parameter name '" + input.Name + "' not unique");
            Add(input);
        }
    }
    public abstract class ParametricEffect : ShaderEffect
    {   // encapsulates pixel shader transformations controled by parameters
        static readonly DependencyProperty InputImage = RegisterPixelShaderSamplerProperty("inputImage", typeof(ParametricEffect), 0, SamplingMode.Bilinear);
        static readonly string packUri = ShaderEffectCompiler.ShaderDir;
        static readonly DependencyProperty MaskProperty = RegisterPixelShaderSamplerProperty("maskImage", typeof(ParametricEffect), 1);
        static protected DependencyProperty[] RegisterParameters(Type type, InputGroup inputs)
        {
            DependencyProperty[] inputProperties = new DependencyProperty[inputs.Count];
            for (int i = 0; i < inputs.Count; i++)
            {
                EffectInput e = inputs[i];
                inputProperties[i] = DependencyProperty.Register(e.Name, e.Type, type, new UIPropertyMetadata(e.Default, PixelShaderConstantCallback(i)));
            }
            return inputProperties;
        }
        static public ParametricEffect Create(EffectType type)
        {
            switch (type)
            {
                case EffectType.ColorAdjustment: return new ColorAdjustmentEffect();
                case EffectType.ColorFilter: return new ColorFilterEffect();
                case EffectType.Edge: return new EdgeEffect();
                case EffectType.GradientContrast: return new GradientContrastEffect();
                case EffectType.ThresholdSmoothing: return new ThresholdSmoothingEffect();
                case EffectType.ViewPoint: return new ViewPointEffect();
                case EffectType.Morph: return new MorphEffect();
            }
            return null;
        }
        public DependencyProperty Parameter(string name, DependencyProperty[] inputProperties)
        {
            foreach (var p in inputProperties)
                if (p.Name == name)
                    return p;
            return null;
        }
        public ImageSource Mask
        {
            get { return ((ImageBrush)GetValue(MaskProperty)).ImageSource; }
            set { SetValue(MaskProperty, new ImageBrush(value)); }
        }
        protected IntSize pixelSize=new IntSize(1000, 1000);
        ShaderEffectCompiler compiler = null;
        public ShaderEffectCompiler Compiler { get { if (compiler == null) compiler = new ShaderEffectCompiler(Type); return compiler; } }
        public virtual DependencyProperty[] Parameters { get; }
        public EffectType Type { get; private set; }
        public string Name { get { return Type.ToString(); } }
        public virtual void SetParameters(ColorTransform trasform, double strength, double level, double size) { }
        public void SetImageSize(IntSize size) { pixelSize = size; }
        public DependencyProperty Parameter(string name)
        {
            foreach (var dp in Parameters)
                if (dp.Name == name)
                    return dp;
            return null;
        }
        public ParametricEffect(EffectType type_, DependencyProperty[] inputProperties)
        {
            Type = type_;
            if (type_ == EffectType.None)
                return;
            try
            {
                UpdateShaderValue(InputImage);
                foreach (DependencyProperty prop in inputProperties)
                    UpdateShaderValue(prop);
                UpdateShader();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to inialize Effect '" + type_ + "': " + ex.Message);
            }
        }
        void UpdateShader()
        {
            PixelShader = new PixelShader();
            PixelShader.UriSource = new Uri(packUri + Type.ToString() + ".ps", UriKind.Absolute);
        }
        public string UpdateShader(string code)
        {   // update code and recompiles shated
            string ret = compiler.UpdateShader(code);
            UpdateShader();
            return ret;
        }
        public void SetParameter(DependencyProperty prop, double v) { if (prop.PropertyType == typeof(double)) SetValue(prop, v); }
        public void SetParameter(DependencyProperty prop, Color c) { if (prop.PropertyType == typeof(Color)) SetValue(prop, c); }
        public void Animate(DependencyProperty prop, DoubleAnimation timeline)
        {
            if (timeline == null)
                return;
            BeginAnimation(prop, timeline);
        }
        //public void Animate(DoubleAnimation[] timeline)
        //{
        //    if (timeline == null) return;
        //    for (int i = 0; i < Math.Min(nParam, timeline.Length); i++)
        //        if (timeline[i] != null) BeginAnimation(inputProperties[i], timeline[i]);
        //}
        public string ToParameterString(DependencyProperty[] inputProperties)
        {
            string pars = "";
            for (int i = 0; i < inputProperties.Length; i++)
                pars += inputProperties[i].Name + " value=" + (inputProperties[i].PropertyType == typeof(double) ?
                    ((double)GetValue(inputProperties[i])).ToString() : ((Color)GetValue(inputProperties[i])).ToString()) + Environment.NewLine;
            return pars;
        }
    }
    public class ColorAdjustmentEffect : ParametricEffect
    {   // bitmap color adjustment; used on bitmaps and bitmap derivatives
        static protected InputGroup inputs = new InputGroup();
        static readonly DependencyProperty[] inputProperties;
        static ColorAdjustmentEffect()
        {
            inputs.AddGroup(InputGroup.bilinear);
            inputs.AddGroup(InputGroup.saturations);
            inputs.AddGroup(InputGroup.opacity);
            inputProperties = RegisterParameters(typeof(ColorAdjustmentEffect), inputs);
        }
        public ColorAdjustmentEffect() : base(EffectType.ColorAdjustment, inputProperties) { }
        public override DependencyProperty[] Parameters { get { return inputProperties; } }
        public override void SetParameters(ColorTransform trasform, double notUsed1, double notUsed2, double notUsed3)
        {
            //Debug.Write("before"+Environment.NewLine+ToParameterString());
            Debug.Assert(trasform.BrightnessValues.Length == 6);
            int i = 0;
            SetParameter(inputProperties[i++], trasform.OldBrightness);
            SetParameter(inputProperties[i++], trasform.NewBrightness);
            SetParameter(inputProperties[i++], trasform.DarkCoef);
            SetParameter(inputProperties[i++], trasform.BrightCoef);
            SetParameter(inputProperties[i++], trasform.RCoef);
            SetParameter(inputProperties[i++], trasform.GCoef);
            SetParameter(inputProperties[i++], trasform.BCoef);
            SetParameter(inputProperties[i++], trasform.Sat);
            Debug.Assert(trasform.TransparencyValues.Length == 4);
            SetParameter(inputProperties[i++], trasform.Opacity);
            SetParameter(inputProperties[i++], trasform.OpacitySlope);
            //Debug.Write("after" + Environment.NewLine + ToParameterString());
        }
    }
    public class ColorFilterEffect : ParametricEffect   // not used
    {
        static protected InputGroup inputs = new InputGroup();
        static readonly DependencyProperty[] inputProperties;
        static ColorFilterEffect()
        {
            inputs.AddGroup(InputGroup.delta);
            inputs.AddGroup(InputGroup.lsh);
            inputs.AddGroup(InputGroup.bilinear);
            inputs.AddColor("Visible");
            inputs.AddColor("Invisible");
            inputProperties = RegisterParameters(typeof(ColorFilterEffect), inputs);
        }
        public ColorFilterEffect() : base(EffectType.ColorFilter, inputProperties) { }
        public override DependencyProperty[] Parameters { get { return inputProperties; } }
    }
    public class EdgeEffect : ParametricEffect  // edges layer
    {
        static protected InputGroup inputs = new InputGroup();
        static readonly DependencyProperty[] inputProperties;
        static EdgeEffect()
        {
            inputs.AddGroup(new InputGroup() { new FloatInput("base", 1), new FloatInput("contrast", 1) });
            inputs.AddGroup(InputGroup.delta);
            //inputs.AddGroup(InputGroup.opacity);
            inputProperties = RegisterParameters(typeof(EdgeEffect), inputs);
        }
        public EdgeEffect() : base(EffectType.Edge, inputProperties) { }
        public override DependencyProperty[] Parameters { get { return inputProperties; } }
        public override void SetParameters(ColorTransform trasform, double strength, double level, double size)
        {
            int i = 0;
            SetParameter(inputProperties[i++], level);
            SetParameter(inputProperties[i++], 4*strength);
            SetParameter(inputProperties[i++], (size + 0.5) * (size / 4 + 0.5) / pixelSize.Width);
            SetParameter(inputProperties[i++], (double)pixelSize.Width / pixelSize.Height);
            //Debug.Write("EdgeEffect parameters" + Environment.NewLine + ToParameterString(inputProperties));
        }
    }
    public class ViewPointEffect : ParametricEffect
    {
        static protected InputGroup inputs = new InputGroup();
        static readonly DependencyProperty[] inputProperties;
        static ViewPointEffect()
        {
            try
            {
                inputs.AddGroup(new InputGroup() {
                    new FloatInput("Distortion", 0),    // view point distortion strength (height / distance)
                    new FloatInput("ViewX", 0),         // view point X relative to center as a fraction of image width (-0.5 - left; 0 - Center; 0.5 - right)
                    new FloatInput("ViewY", 0),         // view point Y relative to center as a fraction of image height (-0.5 - top; 0 - Center; 0.5 - bottom)
                    new FloatInput("Aspect", 0) });     // .Width / .Height
                inputProperties = RegisterParameters(typeof(ViewPointEffect), inputs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        public ViewPointEffect() : base(EffectType.ViewPoint, inputProperties) { }
        public override DependencyProperty[] Parameters { get { return inputProperties; } }
        public override void SetParameters(ColorTransform trasform, double strength, double x, double y)
        {
            Debug.Assert(trasform.BrightnessValues.Length == 6);
            int i = 0;
            SetParameter(inputProperties[i++], strength);
            SetParameter(inputProperties[i++], x);
            SetParameter(inputProperties[i++], y);
            SetParameter(inputProperties[i++], (double)pixelSize.Width / pixelSize.Height);
            //Debug.Write("ViewPointEffect parameters" + Environment.NewLine + ToParameterString(inputProperties));
        }
    }
    public class MorphEffect : ParametricEffect
    {  
        static protected InputGroup inputs = new InputGroup();
        static readonly DependencyProperty[] inputProperties;
        static MorphEffect()
        {
            try
            {
                inputs.AddGroup(new InputGroup() {
                    new FloatInput("Size", 0),          // morph distortion circle radius
                    new FloatInput("CenterX", 0.5),     // morph center point X 
                    new FloatInput("CenterY", 0.5),     // morph center point Y
                    new FloatInput("Aspect", 0),        // .Width / .Height       
                    new FloatInput("ShiftX", 0),        // horisontal shift
                    new FloatInput("ShiftY", 0) });     // vertical shift      
                inputProperties = RegisterParameters(typeof(MorphEffect), inputs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        public MorphEffect() : base(EffectType.Morph, inputProperties) { }
        public override DependencyProperty[] Parameters { get { return inputProperties; } }
        public override void SetParameters(ColorTransform trasform, double size, double x, double y) // x,y: => centerPixel+shift/size (sift<size)
        {
            Debug.Assert(trasform.BrightnessValues.Length == 6);
            int i = 0;
            int px = (int)x;
            int py = (int)y;
            SetParameter(inputProperties[i++], size);
            SetParameter(inputProperties[i++], (double)px / pixelSize.Width);
            SetParameter(inputProperties[i++], (double)py / pixelSize.Height);
            SetParameter(inputProperties[i++], (double)pixelSize.Width / pixelSize.Height);
            SetParameter(inputProperties[i++], x - px);
            SetParameter(inputProperties[i++], y - py);
        }
    }
    public class GradientContrastEffect : ParametricEffect  // sharpness layer
    {   // sharpens or smooths image based on R amd R/2 averaged values: image = C0*original + C1*average(radius/2) + (1-C0-C1)*average(radius)
        static protected InputGroup inputs = new InputGroup();
        static readonly DependencyProperty[] inputProperties;
        static GradientContrastEffect()
        {
            try
            {
                inputs.AddGroup(new InputGroup() {
                    new FloatInput("DX", 0.001), // x-distance (~ delta pixels / width)
                    new FloatInput("Aspect", 1),    // view area width / view area height
                    new FloatInput("C0", 1),     // local amplitude
                    new FloatInput("C1", 0) });  // averaging amplitude
                inputProperties = RegisterParameters(typeof(GradientContrastEffect), inputs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        public GradientContrastEffect() : base(EffectType.GradientContrast, inputProperties) { }
        public override DependencyProperty[] Parameters { get { return inputProperties; } }
        public override void SetParameters(ColorTransform trasform, double strength, double level, double size)
        {   // image = C0*original + C1*average(radius/2) + (1-C0-C1)*average(radius)
            Debug.Assert(trasform.BrightnessValues.Length == 6);
            int i = 0;
            SetParameter(inputProperties[i++], (size + 0.5) * (size / 4 + 0.5) / pixelSize.Width);
            SetParameter(inputProperties[i++], (double)pixelSize.Width / pixelSize.Height);
            SetParameter(inputProperties[i++], 4*strength); // C0
            SetParameter(inputProperties[i++], level+0.33); // C1
            //Debug.Write("GradientContrastEffect"+Environment.NewLine+ ToParameterString(inputProperties));
        }
    }
    public class ThresholdSmoothingEffect : ParametricEffect    // does not work well - not used
    {   // removes odd values if difference from averaged exceeds Level: image = |original-average(radius)|<Level ? original : average(radius)
        static protected InputGroup inputs = new InputGroup();
        static readonly DependencyProperty[] inputProperties;
        static ThresholdSmoothingEffect()
        {
            try
            {
                inputs.AddGroup(new InputGroup() {
                    new FloatInput("DX", 0.001), // x-distance (~ delta pixels / width)
                    new FloatInput("Aspect", 1),    // view area width / view area height
                    new FloatInput("Level", 1) });  // averaging amplitude
                inputProperties = RegisterParameters(typeof(ThresholdSmoothingEffect), inputs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        public ThresholdSmoothingEffect() : base(EffectType.ThresholdSmoothing, inputProperties) { }
        public ThresholdSmoothingEffect(IntSize imSize) : base(EffectType.GradientContrast, inputProperties) { pixelSize = imSize; }
        public override DependencyProperty[] Parameters { get { return inputProperties; } }
        public override void SetParameters(ColorTransform trasform, double strength, double notUsed, double size)
        {   // image = |original-average(radius)|<Level ? original : average(radius)
            Debug.Assert(trasform.BrightnessValues.Length == 6);
            int i = 0;
            SetParameter(inputProperties[i++], size / pixelSize.Width);
            SetParameter(inputProperties[i++], (double)pixelSize.Width / pixelSize.Height);
            SetParameter(inputProperties[i++], strength);
            //Debug.Write("ThresholdSmoothingEffect" + Environment.NewLine + ToParameterString(inputProperties));
        }
    }
    public class ShaderEffectCompiler
    {
        internal static string ShaderDir { get { return Path.Combine(Directory.GetCurrentDirectory(), @"..\Projects\ShaderEffectLib\Shaders\"); } }
        EffectType type;
        string path;
        string sourceCode;
        string SourceFileName       { get { return path + ".fx"; } }
        string ShaderFileName       { get { return path + ".ps"; } }
        public string SourceCode    { get { return sourceCode; } }
        public ShaderEffectCompiler(EffectType effectType)
        {
            type = effectType;
            path = ShaderDir + type; // shader compiler uses '\' as a separator
            string[] sa = new string[0];
            sa = TextFile.Read(SourceFileName);
            StringBuilder text = new StringBuilder();
            foreach (string s in sa)
                text.Append(s + Environment.NewLine);
            sourceCode = text.ToString();
        }
        public string UpdateShader(string effectCode)
        {
            string output = "";
            try
            {
                if (effectCode != null && effectCode.Length > 0)
                {   // updates effect source code
                    sourceCode = effectCode;
                    StreamWriter sw = TextFile.OpenNew(SourceFileName);
                    sw.Write(sourceCode); 
                    sw.Close();
                }
                ProcessStartInfo psi = new ProcessStartInfo(@"C:\Program Files (x86)\Windows Kits\10\bin\x86\fxc.exe");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                //psi.Arguments = string.Format("/Tps_3_0 /Gec /Emain /Fo\"{0}.ps\" \"{0}.fx\"", path);
                psi.Arguments = string.Format("/Tps_2_0 /Emain /Fo\"{0}.ps\" \"{0}.fx\"", path);
                using (Process p = Process.Start(psi))
                {
                    StreamReader sr = p.StandardError;
                    output = sr.ReadToEnd();
                    output = output.Replace(path + ".fx", "Line ");
                    p.WaitForExit();
                }
                return output;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
