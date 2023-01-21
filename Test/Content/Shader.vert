#version 450
#extension GL_KHR_vulkan_glsl: enable

struct ViewProjection {
    mat4 view;
    mat4 projection;
};

layout(binding = 0) uniform wvpBlock {
    ViewProjection viewProjection;
} matrices;
layout(binding = 1) uniform positionsBlock {
    vec4 position[1024];
} positions;

layout(location = 0) in vec3 position;
layout(location = 1) in vec4 color;
layout(location = 2) in vec2 UV;

layout(location = 0) out vec4 fragColor;
layout(location = 1) out vec2 fragTexCoord;

void main() {
    ViewProjection viewProjection = matrices.viewProjection;
    vec4 translated = viewProjection.projection * viewProjection.view * vec4(position + positions.position[gl_InstanceIndex].xyz, 1.0);
    gl_Position = translated;

    fragColor = color;
    fragTexCoord = UV;
}