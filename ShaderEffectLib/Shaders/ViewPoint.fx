float distortion: register(C0);
float viewX: register(C1);
float viewY: register(C2);
float aspect: register(C3);
sampler2D im : register(s0);
float4 main(float2 uv : TEXCOORD) : COLOR
{
  float a = sqrt(aspect);
  float x = (0.5 - uv.x) * a;
  float y = (0.5 - uv.y) / a;
  float sp = x * viewX + y * viewY;
  float vv = viewX * viewX + viewY * viewY;
  uv.x += (sp / vv * viewX - x) * sp / a;
  uv.y += (sp / vv * viewY - y) * sp * a;
  return tex2D(im, uv);
}
