float distortion: register(C0);
float viewX: register(C1);
float viewY: register(C2);
float aspect: register(C3);
sampler2D im : register(s0);
float4 main(float2 uv : TEXCOORD) : COLOR
{
  uv.y+=(0.5-uv.y)*viewX*distortion*(uv.x-0.5);
  uv.x+=(0.5-uv.x)*viewY*distortion*(uv.y-0.5);
  return tex2D(im, uv);
}
