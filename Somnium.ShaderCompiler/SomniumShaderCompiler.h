#pragma once
#include <iostream>
#include <vector>
#include "shaderc/shaderc.hpp"
#include <filesystem>

namespace Somnium
{
    struct ShaderUniformData
    {
    public:
        std::string name;
        uint32_t stride;
        uint32_t binding;
        uint32_t set;
        uint32_t arrayLength;
    };
    struct ShaderSamplerData
    {
    public:
        std::string name;
        uint32_t binding;
        uint32_t set;
        uint32_t arrayLength;
    };
    struct ShaderImageData
    {
    public:
        std::string name;
        uint32_t binding;
        uint32_t set;
        uint32_t arrayLength;
    };

    typedef enum
    {
        None = 0,
        Vertex = 1,
        Fragment = 2,
        TessellationControl = 4,
        TessellationEvaluation = 8,
        Geometry = 16,
        Compute = 32
    } ShaderType;

    struct ShaderCompileResult
    {
    public:
        std::vector<uint32_t> byteCode;
        std::vector<ShaderUniformData> uniforms;
        std::vector<ShaderImageData> images;
        std::vector<ShaderSamplerData> samplers;
        ShaderType type;
    };

	inline constexpr uint32_t shaderFileVersion = 2;

    std::vector<uint32_t> CompileSpirvBinary(const shaderc::Compiler& compiler, const std::string& source_name,
        shaderc_shader_kind kind,
        const std::string& source,
        bool optimize = false);

    std::string file_ReadAllLines(const std::filesystem::path input);

    void GetSpirvUniforms(ShaderCompileResult& compileResult);
}