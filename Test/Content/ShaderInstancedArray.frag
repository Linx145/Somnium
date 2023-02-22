#version 450

layout(binding = 1) uniform sampler samplerState;
layout(binding = 2) uniform texture2D textures[2];//s[6];

layout(location = 0) in vec4 fragColor;
layout(location = 1) in vec2 fragTexCoord;

layout(location = 2) flat in int fragTexID;

layout(location = 0) out vec4 outColor;

void main() {
    outColor = texture(sampler2D(textures[fragTexID]/*[fragTexID]*/, samplerState), fragTexCoord) * fragColor;
}