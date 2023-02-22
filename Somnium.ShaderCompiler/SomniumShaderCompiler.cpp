#include <iostream>
#include <vector>
#include <string>
#include "shaderc/shaderc.hpp"
#include <filesystem>
#include <fstream>
#include <spirv_cross/spirv_cross.hpp>
#include "SomniumShaderCompiler.h"

namespace Somnium
{
    void GetSpirvUniforms(ShaderCompileResult& compileResult)
    {
        std::vector<ShaderUniformData> uniforms;
        std::vector<ShaderSamplerData> samplers;
        std::vector<ShaderImageData> images;
        //std::vector<ShaderImageSamplerData> samplerImages;

        spirv_cross::Compiler crossCompiler = spirv_cross::Compiler(compileResult.byteCode);
        spirv_cross::ShaderResources resources = crossCompiler.get_shader_resources();

        for (auto& uniform : resources.uniform_buffers)
        {
            auto type = crossCompiler.get_type(uniform.type_id);
            
            //result.push_back();
            ShaderUniformData uniformData;
            uniformData.name = uniform.name;
            uniformData.set = crossCompiler.get_decoration(uniform.id, spv::DecorationDescriptorSet);
            uniformData.binding = crossCompiler.get_decoration(uniform.id, spv::DecorationBinding);
            uniformData.stride = crossCompiler.get_declared_struct_size(type);
            if (type.array.size() == 0)
            {
                uniformData.arrayLength = 0;
            }
            else if (type.array.size() > 1)
            {
                std::cerr << "Error: Multidimensional array uniforms not currently supported!" << std::endl;
            }
            else uniformData.arrayLength = type.array[0];

            uniforms.push_back(uniformData);
        }
        for (auto& sampler : resources.separate_samplers)
        {
            auto type = crossCompiler.get_type(sampler.type_id);

            ShaderSamplerData samplerData;
            samplerData.name = sampler.name;
            samplerData.set = crossCompiler.get_decoration(sampler.id, spv::DecorationDescriptorSet);
            samplerData.binding = crossCompiler.get_decoration(sampler.id, spv::DecorationBinding);
        
            if (type.array.size() == 0)
            {
                samplerData.arrayLength = 0;
            }
            else if (type.array.size() > 1)
            {
                std::cerr << "Error: Multidimensional array uniforms not currently supported!" << std::endl;
            }
            else samplerData.arrayLength = type.array[0];

            samplers.push_back(samplerData);
        }
        for (auto& image : resources.separate_images)
        {
            auto type = crossCompiler.get_type(image.type_id);

            ShaderImageData imageData;
            imageData.name = image.name;
            imageData.set = crossCompiler.get_decoration(image.id, spv::DecorationDescriptorSet);
            imageData.binding = crossCompiler.get_decoration(image.id, spv::DecorationBinding);

            if (type.array.size() == 0)
            {
                imageData.arrayLength = 0;
            }
            else if (type.array.size() > 1)
            {
                std::cerr << "Error: Multidimensional array uniforms not currently supported!" << std::endl;
            }
            else imageData.arrayLength = type.array[0];

            images.push_back(imageData);
        }

        /*for (auto& samplerImage : resources.sampled_images)
        {
            auto type = crossCompiler.get_type(samplerImage.type_id);

            ShaderImageSamplerData samplerImageData;
            samplerImageData.name = samplerImage.name;
            samplerImageData.set = crossCompiler.get_decoration(samplerImage.id, spv::DecorationDescriptorSet);
            samplerImageData.binding = crossCompiler.get_decoration(samplerImage.id, spv::DecorationBinding);
            
            samplerImages.push_back(samplerImageData);
        }*/

        //compileResult.samplerImages = samplerImages;
        compileResult.uniforms = uniforms;
        compileResult.samplers = samplers;
        compileResult.images = images;
    }

    std::vector<uint32_t> CompileSpirvBinary(const shaderc::Compiler& compiler, const std::string& source_name,
        shaderc_shader_kind kind,
        const std::string& source,
        bool optimize)
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