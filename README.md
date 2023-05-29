# Somnium.Framework
A Monogame style API that runs on Silk.Net vulkan bindings, made for the Somnium engine. Will support other bindings in the future, including WebGPU. Uses fmod as the audio API, but audio is implemented through interfaces so you can easily swap it out for something of your liking

# Somnium Engine
![EditorPreview2](https://github.com/Linx145/Somnium/assets/32388592/628c3a7f-bcd4-45ca-92fe-c0d61901d3c8)
An experimental WIP 2D ECS engine. Being developed on another repository, will push to this repo after Harpy Raiders' 
demo is released and the engine is stable. Fully capable of producing basic 2D games and more
</br>
</br>
Current features
- ECS with messages, prefabs(Known as blueprints)
- Asset loading
- Full editor with inspector, component editor, entity folders
- Custom limited physics engine made for games that don't need intense physics sims
- Ability to swap out said physics engine for Box2D/anything else
- Mixed lighting (Forward sprite rendering, deferred lighting)
- Basic 3D model loading and support
- Ability to manually interact with the draw API
- Cutscene scripting API
- Particles
- Localization
- Multithreaded rendering
- Tilesets, tile layers
- UI framework
</br>
Planned features

- Multithread the ECS down to the entity level
- WebGPU support
