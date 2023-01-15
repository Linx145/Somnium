#version 450
#extension GL_KHR_vulkan_glsl: enable

layout(binding = 0) uniform UniformBufferObject {
    mat4 world;
    mat4 view;
    mat4 projection;
} ubo;

layout(location = 0) in vec3 position;
layout(location = 1) in vec4 color;
layout(location = 2) in vec2 UV;

layout(location = 0) out vec4 fragColor;
layout(location = 1) out vec2 fragTexCoord;

void main() {
    gl_Position = ubo.projection * ubo.view * ubo.world * vec4(position, 1.0);
    fragColor = color;
    fragTexCoord = UV;
}