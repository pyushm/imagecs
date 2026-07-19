float base: register(C0);
float contrast: register(C1);
float dx: register(C2);
float asp: register(C3);
//float opacity: register(C4);
//float opacitySlope: register(C5);
sampler2D Input : register(s0);
float L2(float4 c) { return c.x*c.x+c.y*c.y+c.z*c.z; }
float Dif2(float2 p, float2 d) { return L2(tex2D(Input, p+d)-tex2D(Input, p-d)); } 
float Dif8(float2 uv)
{
  float dy= dx*asp;
  float dy3= 0.3*dy;
  float dx3= 0.3*dx;
  float2 v0={dx, dy3}, v1={dx3, dy}, v2={-dx3,dy}, v3={-dx, dy3};
  float d=sqrt(Dif2(uv, v0))+sqrt(Dif2(uv, v1))+sqrt(Dif2(uv, v2))+sqrt(Dif2(uv, v3));
  return d/4/dx;
}
float4 main(float2 uv : TEXCOORD) : COLOR
{
  float4 cp=tex2D(Input, uv.xy);
  float c2=contrast*contrast;
  float b=max(0, min(1, base+contrast*(1+0.05*c2)-sqrt(Dif8(uv)/256)*c2));
  float4 color={b,b,b,1};
  return color;
  //return color*(opacity-opacitySlope*b);
}
