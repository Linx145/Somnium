#include <iostream>
#include <vector>
#include <string>
#include "shaderc/shaderc.hpp"
#include <filesystem>
#include <fstream>

namespace Somnium
{
    std::vector<uint32_t> CompileSpirvBinary(const shaderc::Compiler& compiler, const std::string& source_name,
        shaderc_shader_kind kind,
        const std::string& source,
        bool optimize = false)
    {
        shaderc::CompileOptions options;

        if (optimize) options.SetOptimizationLevel(shaderc_optimization_level_performance);
        options.SetTargetEnvironment(shaderc_target_env_vulkan, shaderc_env_version_vulkan_1_2);

        shaderc::SpvCompilationResult module = compiler.CompileGlslToSpv(source, kind, source_name.c_str(), options);

        if (module.GetCompilationStatus() != shaderc_compilation_status_success) {
            std::cerr << module.GetErrorMessage();
            return std::vector<uint32_t>();
        }

        return { module.cbegin(), module.cend() };
    }

    std::string file_ReadAllLines(const std::filesystem::path input)
    {
        std::ifstream file(input);
        if (!file)
            throw std::runtime_error("Could not open file!");

        std::string line;
        std::string allLines;
        while (std::getline(file, line))
        {
            allLines += line;
            allLines.push_back('\n');
        }

        file.close();

        return allLines;
    }
}