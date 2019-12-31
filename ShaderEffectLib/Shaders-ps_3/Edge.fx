float ln: register(C0);
float bl: register(C1);
float cb: register(C2);
float del: register(C3);
float asp: register(C4);
float mix: register(C5);
float opacity: register(C6);
float opacitySlope: register(C7);
float4 sc: register(C11);
float4 sr: register(C12);
sampler2D Input : register(s0);
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
  h.z = h.z/6; h.a = c.a;
  return h;
}
float HueDif(float h) { float d = abs(h); return min(d, 1-d); }
float LSHDif(float4 t) { return abs(t.x)+abs(t.y)+5*HueDif(t.z); }
float L2(float4 c) { return c.x*c.x+c.y*c.y+c.z*c.z; }
float Dif2(float2 p, float2 d)
{
  float4 cp=tex2D(Input, p+d);
  float4 cm=tex2D(Input, p-d);
  float4 dif = cp-cm;
  float4 a=LSH((cp+cm)/2);
  float coef=(ln+LSHDif(sr-a))/(ln+LSHDif(sc-a));
  return L2(dif)*coef;
} 
float Dif8(float2 uv)
{
  float dy= del*asp;
  float2 v0={del,0}, v1={.7*del,.7*dy}, 
         v2={0,dy}, v3={.7*del,-.7*dy};
  float dmax=0, dmin=111;
  for(int i=0; i<4; i++) {
    float2 v = i==0 ? v0 : i==1 ? v1 : i==2 ? v2 : v3;
    float d=Dif2(uv, v);
    dmax=max(dmax, d); dmin=min(dmin, d); }
  return max(dmax-2*dmin, 0);
}
float4 main(float2 uv : TEXCOORD) : COLOR
{
  float4 cp=tex2D(Input, uv.xy);
  float dif = Dif8(uv);
  float b=min(1, bl-sqrt(dif)*cb);
  float4 color={b,b,b,1};
  color = color*(1-mix) + mix*cp;
  return color*(opacity-opacitySlope*b);
}
