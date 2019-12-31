float del: register(C0);
float asp: register(C1);
float level: register(C2);
sampler2D Input : register(s0);
float L2(float4 c) { return c.x*c.x+c.y*c.y+c.z*c.z; }
float4 Average4(float2 c, float dx, float dy)
{ 
  float2 d1={dx, dy*asp};
  float2 d2={-dy, dx*asp};
  return tex2D(Input, c+d1)+tex2D(Input, c-d1)+
         tex2D(Input, c+d2)+tex2D(Input, c-d2);
}
float4 Average8(float2 c, float r)
{
  float d=del*r;
  float d2 = 0.707*d;
  float d3 = 0.707*d;
  float4 av=Average4(c, d, 0)+Average4(c, d2, d2);
  return av/8;
}
float4 main(float2 uv : TEXCOORD) : COLOR
{ 
  float4 av0 = tex2D(Input, uv);
  float4 av1 = Average8(uv, 1);
  av1.a=1;
  return sqrt(L2(av0-av1))<level? av0 : av1;
}
