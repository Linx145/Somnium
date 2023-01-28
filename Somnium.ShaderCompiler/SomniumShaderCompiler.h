#pragma once
#include <iostream>
#include <vector>
#include "shaderc/shaderc.hpp"
#include <filesystem>

namespace Somnium
{
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

	inline constexpr uint32_t shaderFileVersion = 1;

    std::vector<uint32_t> CompileSpirvBinary(const shaderc::Compiler& compiler, const std::string& source_name,
        shaderc_shader_kind kind,
        const std::string& source,
        bool optimize = false);

    std::string file_ReadAllLines(const std::filesystem::path input);

    struct ShaderCompileResult
    {
    public:
        std::vector<uint32_t> byteCode;
        ShaderType type;
    };
}