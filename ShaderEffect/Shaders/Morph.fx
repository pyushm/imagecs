float size: register(C0);
float centerX: register(C1);
float centerY: register(C2);
float aspect: register(C3);
float shiftX: register(C4);
float shiftY: register(C5);
sampler2D im : register(s0);
float4 main(float2 uv : TEXCOORD) : COLOR
{
  float x=(centerX-uv.x)*aspect;
  float y=centerY-uv.y;
  float d=sqrt(x*x+y*y)/size;
  float f=d<0.5 ? 1-2*d*d : d<1 ? 2*(d-1)*(d-1) : 0;
  uv.y+=f*shiftY*size;
  uv.x+=f*shiftX/aspect*size;
  return tex2D(im, uv);
}
