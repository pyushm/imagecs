float dx: register(C0);
float asp: register(C1);
float wl: register(C2);
float ws: register(C3);
float wh: register(C4);
float level: register(C6);
float nl: register(C7);
float rw: register(C8);
float sw: register(C9);
float4 sColor: register(C10);
float4 rColor: register(C11);
sampler2D Input : register(s0);
float4 Average2(float2 c, float2 d)
{ return tex2D(Input, c+d)+tex2D(Input, c-d); }
float4 Average8(float2 c)
{
  float s2=0.707;
  float dy=dx*asp;
  float2 d0={dx,0};
  float2 d1={s2*dx,s2*dy};
  float2 d2={0,dy};
  float2 d3={-s2*dx,s2*dy};
  float4 av=Average2(c,d0)+Average2(c,d1)+Average2(c,d2)+Average2(c,d3);
  return av/8;
}
float4 LSH(float4 c)
{ // x: l; y: s; z: h
  float d=min(min(c.r, c.g), c.b);
  float4 h; // luminocity, saturation, hue, opacity
  if(c.r == c.g && c.r == c.b) { h.x=c.r; h.yz=0; }
  else if(c.b>c.g && c.b>c.r)
    { h.x=c.b; h.y=h.x-d; h.z=1+(c.g-c.r)/h.y; }
  else if(c.g>c.b && c.g>c.r)
    { h.x=c.g; h.y=h.x-d; h.z=3+(c.r-c.b)/h.y; }
  else { h.x=c.r; h.y=h.x-d; h.z=5+(c.b-c.g)/h.y; }
  h.z = h.z/6; h.a = c.a;xx
  return h;
}
float HueDif(float h) { float d = abs(h); return min(d, 1-d); }
float LSHDif(float4 t) { return wl*abs(t.x)+ws*abs(t.y)+wh*HueDif(t.z); }
float4 main(float2 uv : TEXCOORD) : COLOR
{
  float4 color=tex2D(Input, uv.xy);
  float4 c=(color+4*Average8(uv))/5;
  float4 clsh=LSH(c);
  float ds=LSHDif(clsh-sColor);
  float dr=LSHDif(clsh-rColor);
  color.a = 0.5+nl-(ds-level)*sw+(dr-level)*rw;
  return color;
}
