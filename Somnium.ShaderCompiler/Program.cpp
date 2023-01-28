#include <iostream>
#include "shaderc/shaderc.hpp"
#include <vector>
#include <string>
#include <fstream>
#include <filesystem>
#include "nfd/nfd.h"
#include "SomniumShaderCompiler.h"

int main()
{
    shaderc::Compiler compiler;

    while (true)
    {
        nfdpathset_t nfdFilePaths;
        auto result = NFD_OpenDialogMultiple("vert,frag", NULL, &nfdFilePaths);
        if (result != nfdresult_t::NFD_OKAY)
        {
            return 0;
        }

        std::vector<Somnium::ShaderCompileResult> allShaderData;
        std::filesystem::path firstFilePath = NFD_PathSet_GetPath(&nfdFilePaths, 0);
        
        auto pathsCount = NFD_PathSet_GetCount(&nfdFilePaths);

        if (pathsCount > 2)
        {
            std::cerr << "Error: Cannot have more than 2 shader sources per file! These should be either vertex+fragment or tessellation control+tessellation evaluaton." << std::endl;
            return 0;
        }

        for (size_t i = 0; i < pathsCount; i++)
        {
            std::filesystem::path inputFilePath = std::filesystem::path(NFD_PathSet_GetPath(&nfdFilePaths, i));

            auto extension = inputFilePath.extension();
            shaderc_shader_kind GLShaderType;
            Somnium::ShaderType somniumShaderType;
            if (extension == ".vert")
            {
                GLShaderType = shaderc_shader_kind::shaderc_glsl_vertex_shader;
                somniumShaderType = Somnium::ShaderType::Vertex;
                std::cout << somniumShaderType << std::endl;
            }
            else if (extension == ".frag")
            {
                GLShaderType = shaderc_shader_kind::shaderc_glsl_fragment_shader;
                somniumShaderType = Somnium::ShaderType::Fragment;
                std::cout << somniumShaderType << std::endl;
            }
            else
            {
                //don't support compute, tessellation and geometry shaders YET
                std::cerr << "Unrecognised file type: " << extension << std::endl;
                return 0;
            }

            std::string allLines = Somnium::file_ReadAllLines(inputFilePath);

            Somnium::ShaderCompileResult compileResult;
            compileResult.byteCode = Somnium::CompileSpirvBinary(compiler, inputFilePath.filename().string().c_str(), GLShaderType, allLines);
            compileResult.type = somniumShaderType;

            allShaderData.push_back(compileResult);
        }

        auto outputFilePath = firstFilePath.replace_extension(".shader");

        std::cout << "Saving to file: " << outputFilePath << std::endl;

        auto outputFile = std::ofstream(outputFilePath, std::ios::binary | std::ios::out);

        auto totalShaders = (uint64_t)allShaderData.size();

        outputFile.write((char*)&Somnium::shaderFileVersion, sizeof(uint32_t));
        outputFile.write((char*)&totalShaders, sizeof(uint64_t));

        for (size_t i = 0; i < allShaderData.size(); i++)
        {
            uint32_t type = (uint32_t)allShaderData[i].type;
            uint64_t size = (uint64_t)allShaderData[i].byteCode.size();

            outputFile.write((char*)&type, sizeof(uint32_t));
            outputFile.write((char*)&size, sizeof(uint64_t));
            outputFile.write((char*)allShaderData[i].byteCode.data(), sizeof(uint32_t) * allShaderData[i].byteCode.size());
        }

        outputFile.flush();
        outputFile.close();

        std::cout << "Compilation successful!" << std::endl;
    }
}