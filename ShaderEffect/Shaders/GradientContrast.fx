float del: register(C0);
float asp: register(C1);
float a0: register(C2);
float a1: register(C3);
sampler2D Input : register(s0);
float4 Average4(float2 c, float dx, float dy)
{ 
  float2 d1={dx, dy*asp};
  float2 d2={-dy, dx*asp};
  float4 v1=tex2D(Input, c+d1);
  float4 v2=tex2D(Input, c-d1);
  float4 v3=tex2D(Input, c+d2);
  float4 v4=tex2D(Input, c-d2);
  float4 av=v1+v2+v3+v4;
  /*av.a=v1.a*v2.a*v3.a*v4.a;*/
  return av;
}
float4 Average8(float2 c, float r)
{
  float d=del*r;
  float d2 = 0.707*d;
  float4 v1=Average4(c, d, 0);
  float4 v2=Average4(c, d2, d2);
  float4 av=v1+v2;
  /*av.a=v1.a*v2.a;*/
  return av/8;
}
float4 main(float2 uv : TEXCOORD) : COLOR
{ 
  float4 orig = tex2D(Input, uv);
  float4 v1=Average8(uv, 0.5);
  float4 v2=Average8(uv, 1);
  float4 con = a0*orig +a1*v1+(1-a0-a1)*v2;
  con.a = orig.a;
  return con;
}
