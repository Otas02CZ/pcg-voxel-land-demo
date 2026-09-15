# Procedural Generator of 3D Voxel Maps

This project is a demonstration of procedurally generated voxel environments in the Godot Engine. It was originally developed for my bachelor's thesis at Brno University of Technology, Faculty of Information Technology, and this repository represents an enhanced, up-to-date version.

The original version of the project can be found at the [initial commit](https://github.com/Otas02CZ/pcg-voxel-land-demo/tree/9ad05b508f59c7e7d71fb91237d68ef4ef3624ad) of this repository. The bachelor's thesis and its grading can be found on the university [website](https://www.vut.cz/en/students/final-thesis/detail/170297). All originally submitted files are located in [this](https://github.com/Otas02CZ/bachelors-thesis-pcg-voxel-land-demo) repository. This project also participated in the [Excel@FIT](https://excel.fit.vutbr.cz/) conference.

This project aims to iteratively generate procedural, pseudo-infinite micro-voxel environments. It mainly focuses on semi-realistic natural landscapes with high voxel detail, somewhere between games such as Minecraft and Lay of the Land. The environments are fully procedurally generated without any static assets. The generation is, of course, deterministic and depends on a set of configurable parameters (seed, terrain height, vegetation settings, water, caves, ...). The project also offers a basic user interface, world exploration options, voxel editing with persistence, application configuration options, and simple time and weather simulation.

## Showcase

## Video on YouTube

[![Demonstration Video](https://img.youtube.com/vi/Znaj-E4F0AI/maxresdefault.jpg)](https://www.youtube.com/watch?v=Znaj-E4F0AI)

## Screenshots

![Mountain meadow](/docs/00_main.jpg)
![Mountain and snowy areas](/docs/0102.jpg)
![Caves and rain in mountains](/docs/0304.jpg)
![Snowy mountains, mountains at night](/docs/0506.jpg)
![Voxel editing, mountains at night with rain](/docs/0708.jpg)
![Mountains at night, panorama](/docs/0910.jpg)
![Lake with river, forest](/docs/1112.jpg)

## Project Manual

The thesis contains most of the important information about the project, including relevant research, the design of the procedural generator, and the architectural design and implementation of the project in C# and the Godot Engine.

However, some basic information regarding project features, a basic overview of the procedural methods used, and an overview of the application architecture can be found below. Also, pay attention to the changes from the original version, as some parts of the thesis are now outdated.

More information can be found in the project source files, containing pieces of documentation, most notably the architectural overview in [Root.cs](https://github.com/Otas02CZ/pcg-voxel-land-demo/blob/master/Scripts/Root.cs), as well as in the originally submitted project [readme](https://github.com/Otas02CZ/bachelors-thesis-pcg-voxel-land-demo).

### Features

- Fully procedurally generated voxel environments.
    - Diverse landscapes (valleys, mountains, plateaus)
    - Semi-realistic custom voxel models.
    - Semi-realistic water features, based on terrain properties.
    - Diverse and semi-realistic biomes, with classification driven by terrain properties and water availability.
    - Cave systems.
- Custom highly parallelized chunk system for the generation, displaying, and unloading of
pseudo-infinite worlds via a dynamic hierarchical system of distributed tasks
- Basic exploration options, including movement on the terrain surface, collision-free flying, and teleportation.
- Persistent voxel editing.
- Support for multiple persistent worlds.
- Simple time of day simulation.
- Simple weather simulation with basic phenomena.
- Complex configurable options of world creation.
- Basic user interface and application settings.

### Procedural Methods Overview

- Procedural noise (terrain, caves, rock models, sky).
- Pseudo-random number generators (small plant models, model placement).
- Space Colonization algorithm (models of trees and bushes).
- Priority-Flood algorithm (lake extraction).
- Terrain gradients (rivers).

### Architectural Overview

The world is divided into chunks that are managed by a custom hierarchical chunk system. This system decides which chunks, at which hierarchical layers, should be loaded, displayed, or unloaded based on the current camera position in world space.

Chunk System Layer Overview:
1. Pre-generation of voxel models.
2. World Generation of large rectangular regions. Mostly 2D information.
3. Voxelization of world information and placement of voxel models into voxel storage utilizing Sparse Voxel Octrees.
4. Geometry extraction from the voxel space, using the Greedy Meshing algorithm. Two surfaces (solids, water).
5. Display of the chunk geometry in the Godot Engine with `MeshInstance3D` and `StaticBody3D` nodes.

The rest of the implementation handles user interface events, editing operations, weather and time simulation, permanent storage, and more.

### Changes from the Original Version

- Fix for rendering artifacts on Nvidia GPUs due to floating-point precision errors.
- Slight Greedy Meshing optimizations
- Chunk geometry generation on a per LOD basis, instead of generating all LODs of a chunk at once.
- Support for 4th LOD.
- Support for in-game render distance changing.
- Improvements to stuttering
  - Collision geometry uses LOD2 instead of the original LOD1.
  - Partial decouple of the mesh application from the main thread.
  - The mesh application is divided into column chunks and distributed over time.
- Bumped Godot Engine version to 4.7.2.
- Switch to circular mapping of active chunks from the original rectangular one.
- Explicit garbage collection on world exit.
- Improvements to procedural generation:
  - A higher number of models, which are also more distinct from each other.
  - More natural environments, due to changes in model placement.
- Support for switching fullscreen / windowed mode.
- Finalized the incomplete time of day simulation.
- Added procedurally generated sky.
- Added weather simulation:
  - Interacts with the sky, changing cloud density and colors.
  - Basic phenomena: rain, snow, and dust in caves.
  - Particle systems.
- Improved voxel editing:
  - In-game hotbar of voxel types.
  - Inventory-like selection of voxel types in the hotbar.
  - Hotkeys for switching voxel types and sizes.
- More states are saved in the world config, including:
  - Camera rotation.
  - Torch state.
  - Weather and time of day states.
  - Selected voxel types and size.
- Many fixes and general improvements, including thread safety, parallelization tweaks, ...
- And more ...

The application now requires roughly half the memory and less than half the CPU time compared with the original version. Stuttering is also greatly reduced.

### Possible Further Work

- Experiments with other approaches to voxel storage (RLE, hybrid methods, ...).
- Optimizations of geometry extraction (maybe greedy meshing?, ...).
- Fixing missing faces at LOD transitions.
- More realistic vegetation, terrain, and placement of features.
    - Simulation of plant ecosystem evolution, rather than random placement.
    - Terrain erosion.
    - Tree generation with Shadow Propagation techniques.
- Switch to Direct Voxel Rendering and use of ray casting, instead of geometry extraction and display.
- ...


### Dependencies

The project was developed and tested in Godot Engine 4.7.2 and implemented in C#, targeting .NET SDK 10. Thus, all that is required to run and export the project is an official engine build with c# support, and an up-to-date .NET SDK.

The project also uses a single third-party extension [Debug Menu](https://github.com/godot-extended-libraries/godot-debug-menu), which is embedded in the `addons` folder.

## Application Manual


### Demo User Interface

Once opened, the application displays the title menu, from which the user can access the settings or worlds sub-menus. The settings sub-menu allows configuration of the demo visuals and render distances.

The world sub-menu allows creation of new worlds as well as opening or deletion of existing ones. To create a new world, simply press the large `Create New World` button, which takes you to the world creation sub-menu. Here, choose a name and a world generation preset (Settings Profile), then press the `Create` button. If you wish, you can also adjust detailed world generator parameters, but note that some options, such as terrain height and cave generation, can significantly increase memory usage and reduce overall performance.

When a world is selected and opened, or is being created, the application shows a rudimentary loading screen that is hidden once the first geometry column is generated and displayed. It is normal that upon entering the world space, only a few columns are rendered, and it may take some time for most of the area around the camera to be ready.

### In-Game Controls

The camera can be moved with either `W`/`A`/`S`/`D` or arrow keys. Camera rotation is done with mouse movement. To move up and down, you can use the `Q`/`E` keys. Note that all movement is rotation-dependent. To toggle between the default fly mode and walking, use the `F` key. When walking, you can also jump with `Space`. To move faster, hold the `Left Shift` key, but note that, especially in high render distances, the chunk system will not be able to keep up with fast movement. You can also toggle a torch-like light source with `T`. Time simulation (day-night cycle) can be toggled with `R`, and weather simulation with `Z`. To place and delete voxels in the area of the cross-hair, use the `Left Mouse` button for deletion and the `Right Mouse` button for placement. The menu bar at the bottom of the screen shows the currently selected voxel types and voxel size. To cycle through them, use `Mouse Wheel Shift`/`1`/`2`. To change the voxel size, use `Mouse Wheel Click`/`3`/`4`. When you press the `Escape` key, you get to the pause sub-menu. Here you can leave the world, read the controls, access application settings, and most importantly, configure voxel editing and use teleportation.

The voxel size setting determines the size of the voxel cubes. Number 1 denotes the smallest cube (plant voxels), and 7 denotes the largest cube, which fills a whole chunk. Here, you can also change the selected voxel types in the hotbar by simply dragging them from the list of available types.

The teleportation menu can be used to quickly move to a specified X, Z position in the world in meters from the world origin. Note that after teleportation, the demo will need some time before the new area is generated and starts rendering.

There are also some debugging hotkeys. Key `Y` shows the wireframe, and the keys `F2` and `F3` can be used to display the chunk system and other statistics, as well as the performance overlay, respectively.


### Hardware Requirements

The application is quite performance-heavy, especially in terms of memory usage and CPU time, particularly when configured with high render distances. The world configuration can also heavily influence the application performance. Pay attention to increasing world height, as well as cave environment, and vegetation generation settings.

The default configuration is an optimized preset with a `Very Low` render distance, which should run on practically any modern device with at least 4 - 6 GB of RAM, a quad-core CPU and GPU with at least 2-3 GB of VRAM, and support for Vulkan or DirectX 12.

To further reduce pressure on RAM and CPU utilization, you can choose the world preset `Default Low Spec`. If you are struggling with GPU performance, try the scaling options and tune down SDFGI and other rendering features in the settings. This will, however, significantly impact the visuals.

To run the application at higher render distances, such as `Medium` and `High`, you will need 12 - 16 GB of RAM, hexa-core or ideally an octa-core CPU, and a decent modern GPU. Render distances at or above the `Ultra` preset should be considered experimental and require 32+ GB of RAM, a fast multi-core CPU and fast GPU.

Tested devices under Fedora Linux and Windows 11:
- 48 GB, R9 5900XT, RX 7700XT - high, ultra preset
- 32 GB, Intel 155H, iGPU, see known issues
- 16 GB, i7 4770K, GTX 1060 6GB - very low, low preset

The application was tested only on x86 CPUs and Fedora 44 / Windows 11. Please note that ARM and macOS builds were not tested at all, as I do not have access to these devices. You may encounter some issues here. On top of that, the macOS builds have disabled notarization and lack correct signatures (sorry, no Apple Developer ID).

### Known Issues

It was observed that some modern integrated Intel GPUs (such as the iGPU in the Intel Core Ultra 7 155H) render water surfaces incorrectly under Windows when the Forward+ renderer is used. However, when the project is run on these devices with the same configuration under Linux, no issues occur. If you observe these or similar issues, you can switch the renderer to Mobile in the Godot Engine editor. However, renderers other than Forward+ were not tested, and visual quality will be significantly reduced due to the lack of SDFGI and other rendering features.

The demo is very performance-heavy. On weak devices, it may take more than a minute for the world to initialize and start rendering. Similarly, stuttering during movement, as well as slow chunk column generation, slow LOD updates, and slow updates of chunks where you edited geometry, can be observed.

You can also see missing faces at the LOD transitions of chunks.

It is possible that there still exist some issues in the chunk system, resulting in missing chunks, etc. If you encounter such or other problems, please submit an issue on this repository.

If Godot Engine fails to register the configured main scene, select and run the scene `Scenes/Root.tscn` when attempting to run the project from the editor.

### Stored Data

The demo stores its data permanently in user space in a folder managed by the Godot Engine. To see the actual paths on your system, visit: [docs.godotengine.org/en/stable/tutorials/io/data_paths.html](https://docs.godotengine.org/en/stable/tutorials/io/data_paths.html). The actual name of the project sub-folder is `PCGVoxelLandscapes`. Along with engine-related files and folders, it contains a `config.json` file with the application configuration and a `worlds` folder that stores data of all created worlds (world configuration and chunk voxel changes).


## AI Usage Declaration

AI language models were used to a limited extent for text spelling and grammatical corrections, research, implementation of minimal feature prototypes, and occasionally for inline editor auto-completion and debugging.

The resulting design, application architecture, implementation, and thesis are my own work.

## Acknowledgments

I would like to thank my supervisor Ing. Michal Vlnas for his valuable help and advice given throughout the countless consultations.