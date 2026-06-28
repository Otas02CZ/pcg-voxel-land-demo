// FILE: StorageService.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains service class that encapsulates permanent storage IO operations.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

namespace PCGVoxelLandscapes.Scripts;

/**
 * StorageService class for encapsulation of permanent storage IO operations.
 * Supports loading / saving app configuration and working with demo worlds.
 */
public class StorageService
{
    private readonly string configFileName = "config.json";
    private readonly string worldsDirectoryName = "worlds";
    private readonly string worldConfigFileName = "world.dat";
    private readonly string worldDataIndexFileName = "data_index.dat";
    private readonly string worldDataDirectoryName = "data";
    
    private readonly string configFilePath;
    private readonly string worldsDirectoryPath;

    private string currentWorldDirectoryPath;
    private string currentWorldDataIndexFilePath;
    private string currentWorldDataDirectoryPath;
    private FileStream currentWorldDataIndexFileStream;

    // index of edited columns in this world
    private readonly ConcurrentDictionary<(int chunkX, int chunkZ), string> chunkColumnDataIndex;
    
    /**
     * Initialize storage, create main directory if it does not exist.
     */
    public StorageService(string storageDirectoryPath)
    {
        configFilePath = Path.Combine(storageDirectoryPath, configFileName);
        worldsDirectoryPath = Path.Combine(storageDirectoryPath, worldsDirectoryName);
        chunkColumnDataIndex = new ConcurrentDictionary<(int chunkX, int chunkZ), string>();
        
        try
        {
            Directory.CreateDirectory(worldsDirectoryPath);
        } catch (IOException ex)
        {
            GD.PrintErr($"Failed to create storage directories: {ex.Message}");
            throw new Exception("Failed to create storage directories");
        }
    }

    /**
     * Attempts to load data index of currently selected world into memory.
     * Index file format:
     * chunkX chunkZ dataFileName\n
     */
    private bool LoadWorldDataIndex()
    {
        try
        {
            chunkColumnDataIndex.Clear();
            // open data index file
            using (StreamReader reader = new StreamReader(currentWorldDataIndexFileStream, leaveOpen: true))
            {
                string line;
                while ((line = reader.ReadLine()) != null) // read and parse it line by line
                {
                    string[] parts = line.Split(' ');
                    if (parts.Length == 3 &&
                        int.TryParse(parts[0], out int chunkX) &&
                        int.TryParse(parts[1], out int chunkZ))
                    {
                        string dataFileName = parts[2];
                        chunkColumnDataIndex[(chunkX, chunkZ)] = dataFileName;
                    }
                }
            }
            return true;
        }
        catch (IOException ex)
        {
            GD.PrintErr($"Failed to load world data index: {ex.Message}");
            return false;
        }
    }

    /**
     * Saves new voxel operation to data file of given chunk column.
     * Operation entry format:
     * worldX worldY worldZ voxelSize voxelType\n
     */
    public bool SaveVoxelOperation(int chunkX, int chunkZ, int worldX, int worldY, int worldZ, byte voxelSize, VoxelType voxelType)
    {
        // check if chunk column already has data file
        string fileName;
        if (!chunkColumnDataIndex.ContainsKey((chunkX, chunkZ)))
        {
            // if not create new file and add entry to index
            fileName = $"chunk_column_{chunkX}_{chunkZ}.dat";
            chunkColumnDataIndex[(chunkX, chunkZ)] = fileName;
            string newIndexEntry = $"{chunkX} {chunkZ} {fileName}";
            try
            {
                // write it immediately
                using (StreamWriter writer = new StreamWriter(currentWorldDataIndexFileStream, leaveOpen: true))
                {
                    writer.WriteLine(newIndexEntry);
                }
            }
            catch (IOException ex)
            {
                GD.PrintErr($"Failed to update world data index: {ex.Message}");
                return false;
            }
        } else {
            fileName = chunkColumnDataIndex[(chunkX, chunkZ)];
        }
        // attempt to open chunk column data file, append new voxel operation
        string dataFilePath = Path.Combine(currentWorldDataDirectoryPath, fileName);
        string operationEntry = $"{worldX} {worldY} {worldZ} {voxelSize} {voxelType}";
        try
        {
            using (StreamWriter writer = new StreamWriter(dataFilePath, append: true))
            {
                writer.WriteLine(operationEntry);
            }
            return true;
        } catch (IOException ex)
        {
            GD.PrintErr($"Failed to save voxel operation: {ex.Message}");
            return false;
        }
    }

    /**
     * Apply all stored voxel operations for given chunk column, if any exist.
     * Operation entry format:
     * worldX worldY worldZ voxelSize voxelType\n
     */
    public bool LoadApplyChunkColumnVoxelOperations(ChunkColumn chunkColumn)
    {
        // check whether chunkColumnDataIndex contains entry for this chunk column
        if (!chunkColumnDataIndex.ContainsKey((chunkColumn.chunkX, chunkColumn.chunkZ)))
        {
            return true;
        }
        
        // data exists
        string dataFileName = chunkColumnDataIndex[(chunkColumn.chunkX, chunkColumn.chunkZ)];
        string dataFilePath = Path.Combine(currentWorldDataDirectoryPath, dataFileName);
        try
        {
            // open file
            using (StreamReader reader = new StreamReader(dataFilePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null) // read and parse it line by line
                {
                    string[] parts = line.Split(' ');
                    if (parts.Length == 5 &&
                        int.TryParse(parts[0], out int worldX) &&
                        int.TryParse(parts[1], out int worldY) &&
                        int.TryParse(parts[2], out int worldZ) &&
                        byte.TryParse(parts[3], out byte voxelSize) &&
                        Enum.TryParse(parts[4], out VoxelType voxelType))
                    {
                        // apply each voxel operation
                        chunkColumn.SetVoxel(worldX, (uint)worldY, worldZ, voxelType, voxelSize);
                    }
                }
            }
            return true;
        }
        catch (IOException ex)
        {
            GD.PrintErr($"Failed to load and apply chunk column data: {ex.Message}");
            return false;
        }
    }

    /**
     * Loads and returns app config without parsing
     */
    public String LoadApplicationConfig()
    {
        try {
            if (!File.Exists(configFilePath))
            {
                // config not found
                GD.PrintErr($"Config file not found");
                return null;
            }
            using (StreamReader reader = new StreamReader(configFilePath))
            {
                // read it
                string configText = reader.ReadToEnd();
                return configText;
            }
        } catch (IOException ex)
        {
            GD.PrintErr($"Failed to load application config: {ex.Message}");
            return null;
        }
    }
    
    /**
     * Save serialized application config to file
     */
    public bool SaveApplicationConfig(string configText)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(configFilePath, false))
            {
                // write it all
                writer.Write(configText);
                return true;
            }
        } catch (IOException ex)
        {
            GD.PrintErr($"Failed to save application config: {ex.Message}");
            return false;
        }
    }

    /**
     * Obtain list of all saved worlds.
     * Worlds saved in its own subdirectories, each has config file.
     * Loops through all directories and attempts to parse their config files.
     */
    public List<WorldSaveConfig> ObtainSavedWorldsList()
    {
        List<WorldSaveConfig> worlds = new List<WorldSaveConfig>();

        try
        {
            if (!Directory.Exists(worldsDirectoryPath))
            {
                GD.PrintErr($"Worlds directory not found");
                return worlds;
            }
            // scan worlds directory for subdirectories
            string[] subdirectories = Directory.GetDirectories(worldsDirectoryPath);
            
            // go through subdirectories
            foreach (string subdirectory in subdirectories)
            {
                string worldConfigFilePath = Path.Combine(subdirectory, worldConfigFileName);
                try
                {
                    if (!File.Exists(worldConfigFilePath))
                    {
                        GD.PrintErr($"World config file not found in {subdirectory}, skipping");
                        continue;
                    }
                    // read config file
                    string configText;
                    using (StreamReader reader = new StreamReader(worldConfigFilePath))
                    {
                        configText = reader.ReadToEnd();
                    }
                    // deserialize it
                    WorldSaveConfig worldSaveConfig = JsonSerializer.Deserialize<WorldSaveConfig>(configText);
                    if (worldSaveConfig == null)
                    {
                        GD.PrintErr($"Failed to load world config in {subdirectory}, skipping");
                        continue;
                    }

                    worlds.Add(worldSaveConfig);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"Failed to load world config in {subdirectory}, skipping. Reason: {ex.Message}");
                }
            }
            
            return worlds;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to obtain saved worlds list: {ex.Message}");
            return worlds;
        }
    }

    /**
     * Attempts to load world with given directory name.
     * Duplicates some operations of ObtainSavedWorldsList to ensure nothing has changed.
     */
    public WorldSaveConfig LoadSelectedWorld(string worldDirectoryName)
    {
        string worldDirectoryPath = Path.Combine(worldsDirectoryPath, worldDirectoryName);
        string worldConfigFilePath = Path.Combine(worldDirectoryPath, worldConfigFileName);
        string worldDataIndexFilePath = Path.Combine(worldDirectoryPath, worldDataIndexFileName);
        
        try
        {
            if (!Directory.Exists(worldDirectoryPath)) 
            {
                GD.PrintErr($"World directory not found for world directory {worldDirectoryName}");
                return null;
            }
            // open world config file
            if (!File.Exists(worldConfigFilePath))
            {
                GD.PrintErr($"World config file not found for world directory {worldDirectoryName}");
                return null;
            }
            string configText;
            // read the config
            using (StreamReader reader = new StreamReader(worldConfigFilePath))
            {
                configText = reader.ReadToEnd();
            }
            // parse it
            WorldSaveConfig worldSaveConfig = JsonSerializer.Deserialize<WorldSaveConfig>(configText);
            if (worldSaveConfig == null)
            {
                GD.PrintErr($"Failed to deserialize world config for world directory {worldDirectoryName}");
                return null;
            }

            // validate thresholds for age, water and height configurations
            if (!worldSaveConfig.worldSettings.ValidateThresholdsArrays())
            {
                GD.PrintErr($"World config file contains invalid thresholds array");
                return null;
            }
            // ensure all values are withing limits
            worldSaveConfig.worldSettings.ClampAll();
            
            // setup current world paths
            currentWorldDirectoryPath = worldDirectoryPath;
            currentWorldDataIndexFilePath = worldDataIndexFilePath;
            currentWorldDataDirectoryPath = Path.Combine(worldDirectoryPath, worldDataDirectoryName);
            
            // setup data index file and stream
            if (!File.Exists(currentWorldDataIndexFilePath))
            {
                GD.PrintErr($"World data index file not found for world {worldSaveConfig.worldName}, creating new one");
                currentWorldDataIndexFileStream = File.Open(currentWorldDataIndexFilePath, FileMode.OpenOrCreate);
                chunkColumnDataIndex.Clear();
            } else
            {
                currentWorldDataIndexFileStream = File.Open(currentWorldDataIndexFilePath, FileMode.Open);
                if (!LoadWorldDataIndex())
                {
                    GD.PrintErr($"Failed to load world data index for world {worldSaveConfig.worldName}");
                    return null;
                }
            }
            
            // create world directory if it does not exist
            if (!Directory.Exists(currentWorldDataDirectoryPath)) 
            {
                GD.PrintErr($"World data directory not found for world {worldSaveConfig.worldName}, creating new one");
                Directory.CreateDirectory(currentWorldDataDirectoryPath);
            }
            
            return worldSaveConfig;
        }
        catch (IOException ex)
        {
            GD.PrintErr($"Failed to load selected world directory {worldDirectoryName}: {ex.Message}");
            return null;
        }
    }

    /**
     * Creates new world directory and files. Sets it up as current.
     */
    public bool CreateWorld(WorldSaveConfig worldSaveConfig)
    {
        string worldDirectoryPath = Path.Combine(worldsDirectoryPath, worldSaveConfig.worldDirectoryName);
        string worldConfigFilePath = Path.Combine(worldDirectoryPath, worldConfigFileName);
        string worldDataIndexFilePath = Path.Combine(worldDirectoryPath, worldDataIndexFileName);
        string worldDataDirectoryPath = Path.Combine(worldDirectoryPath, worldDataDirectoryName);
        
        // create directories and files
        try
        {
            Directory.CreateDirectory(worldDirectoryPath);
            Directory.CreateDirectory(worldDataDirectoryPath);
            
            using (StreamWriter writer = new StreamWriter(worldConfigFilePath, false))
            {
                string configText = JsonSerializer.Serialize(worldSaveConfig);
                writer.Write(configText);
            }
            
            // setup current paths
            currentWorldDirectoryPath = worldDirectoryPath;
            currentWorldDataIndexFilePath = worldDataIndexFilePath;
            currentWorldDataDirectoryPath = worldDataDirectoryPath;
            
            // create and setup index
            currentWorldDataIndexFileStream = File.Open(currentWorldDataIndexFilePath, FileMode.OpenOrCreate);
            chunkColumnDataIndex.Clear();
            
            return true;
        }
        catch (IOException ex)
        {
            GD.PrintErr($"Failed to create world: {ex.Message}");
            return false;
        }
    }
    
    /**
     * Saves current world configuration into currently loaded world directory.
     * Mainly for saving updated player position when leaving world.
     */
    public void SaveCurrentWorldConfig(WorldSaveConfig worldSaveConfig)
    {
        string worldConfigFilePath = Path.Combine(currentWorldDirectoryPath, worldConfigFileName);
        try
        {
            // save the config file
            using (StreamWriter writer = new StreamWriter(worldConfigFilePath, false))
            {
                string configText = JsonSerializer.Serialize(worldSaveConfig);
                writer.Write(configText);
            }
        }
        catch (IOException ex)
        {
            GD.PrintErr($"Failed to save current world config: {ex.Message}");
        }
    }
    
    /**
     * Deletes world in given directory
     */
    public void DeleteWorld(string worldDirectoryName)
    {
        string worldDirectoryPath = Path.Combine(worldsDirectoryPath, worldDirectoryName);
        try
        {
            Directory.Delete(worldDirectoryPath, true);
        }
        catch (IOException ex)
        {
            GD.PrintErr($"Failed to delete world directory {worldDirectoryName}: {ex.Message}");
        }
    }

    /**
     * Closes and flushes opened index file stream.
     */
    public void Dispose()
    {
        if (currentWorldDataIndexFileStream != null)
        {
            currentWorldDataIndexFileStream.Flush();
            currentWorldDataIndexFileStream.Close();
            currentWorldDataIndexFileStream = null;
        }
    }
}
