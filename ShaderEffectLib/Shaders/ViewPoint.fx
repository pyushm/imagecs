float distortion: register(C0);
float viewX: register(C1);
float viewY: register(C2);
float aspect: register(C3);
sampler2D im : register(s0);
float4 main(float2 uv : TEXCOORD) : COLOR
{
  float2 view={viewX+0.5, viewY+0.5};
  float2 d=uv-view;
  float f=sqrt(d.x*d.x+d.y*d.y)*distortion;
  float c= f==0 ? 0 : atan(f)/f-1;
  if(distortion<0) c=-c;
  uv=d*(c+distortion/10)+uv;
  return tex2D(im, uv);
}
