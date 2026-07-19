float oldl: register(C0);
float newl: register(C1);
float dcoef: register(C2);
float bcoef: register(C3);
float satr: register(C4);
float satg: register(C5);
float satb: register(C6);
float sat: register(C7);
float opacity: register(C8);
float opacitySlope: register(C9);
sampler2D Input : register(s0);
float4 main(float2 uv : TEXCOORD) : COLOR
{
  float4 orig=tex2D(Input,uv.xy);
  float a=orig.a;
  if(a>0)
    orig.rgb=orig.rgb/a;
  float l=max(max(orig.r, orig.g), orig.b)+0.0001f;
  float ln=l>oldl ? newl+bcoef*(l-oldl ) : newl+dcoef*(l-oldl );
  float tCoef = opacity-opacitySlope*l;
  float brCoef=ln/l * tCoef;
  orig.r =(l-(l-orig.r)*sat)*brCoef*satr*a;
  orig.g =(l-(l-orig.g)*sat)*brCoef*satg*a;
  orig.b =(l-(l-orig.b)*sat)*brCoef*satb*a;
  orig.a = orig.a*tCoef;
  return orig;
}
