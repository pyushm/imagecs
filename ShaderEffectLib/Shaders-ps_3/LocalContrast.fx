float del: register(C0);
float asp: register(C1);
float loc: register(C2);
float av: register(C3);
float pos: register(C4);
float neg: register(C5);
float maxDif: register(C6);
float base: register(C7);
sampler2D Input : register(s0);
float4 av0, av1, av2, av3, av4;
float L2(float4 c) { return c.x*c.x+c.y*c.y+c.z*c.z; }
float4 Average2(float2 c, float2 d)
{ return tex2D(Input, c+d)+tex2D(Input, c-d); }
float4 Average8(float2 c, int r)
{
  float s2=0.707;
  float dx=del*r;
  float dy=dx*asp;
  float2 d0={dx,0};
  float2 d1={s2*dx,s2*dy};
  float2 d2={0,dy};
  float2 d3={-s2*dx,s2*dy};
  float4 av=Average2(c, d0)+Average2(c, d1)+
            Average2(c, d2)+Average2(c, d3);
  return av/8;
} // line 
float4 TFilter(float r)
{ 
  float w=r+1;
  float4 res = (r+1)*av0;
  if(r>0) { w+=r; res+=r*av1; }
  if(r>1) { w+=r-1; res+=(r-1)*av2; }
  if(r>2) { w+=r-2; res+=(r-2)*av3; }
  if(r>3) { w+=r-3; res+=(r-3)*av4; }
  return res/w;
} 
float4 main(float2 uv : TEXCOORD) : COLOR
{ 
  av0 = tex2D(Input, uv);
  av1 = Average8(uv, 1)+Average8(uv, 2)-Average8(uv, 2);
  av2 = Average8(uv, 2)+Average8(uv, 2)-Average8(uv, 2);
  av3 = Average8(uv, 3)+Average8(uv, 2)-Average8(uv, 2);
  av4 = Average8(uv, 4)+Average8(uv, 2)-Average8(uv, 2);
  float4 res=(av+loc)*TFilter(pos) - av*TFilter(neg);
  res.a=1;
  if(maxDif<=0)
    return res;
  return sqrt(L2(av0-res))<maxDif ? av0 : res;
}
