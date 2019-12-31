float s: register(C0);
float viewX: register(C1);
float viewY: register(C2);
float ratio: register(C3);
float offx: register(C4);
float offy: register(C5);
float scalex: register(C6);
float scaley: register(C7);
sampler2D im : register(s0);
float4 main(float2 uv : TEXCOORD) : COLOR
{
  float  dx=(uv.x-viewX-0.5)*ratio;
  float  dy=uv.y-viewY-0.5;
  float f=sqrt(dx*dx+dy*dy)*s;
  float c= f==0 ? 0 : atan(f)/f-1;
  if(s<0) c=-c;
  uv.x+=dx*c/ratio+offx-(scalex-1)*(uv.x-0.5);
  uv.y+=dy*c+offy-(scaley-1)*(uv.y-0.5);
  return tex2D(im, uv.xy);
}
