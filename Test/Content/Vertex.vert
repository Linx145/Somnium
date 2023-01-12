#version 450
#extension GL_KHR_vulkan_glsl: enable

layout(location = 0) in vec3 position;
layout(location = 1) in vec4 color;
layout(location = 0) out vec4 fragColor;

void main() {
    gl_Position = vec4(position, 1.0);//vec4(positions[gl_VertexIndex], 0.0, 1.0);
    fragColor = color;
}