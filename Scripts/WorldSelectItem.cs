// FILE: WorldSelectItem.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains class of GUI node for display of one saved world

using Godot;
using System;

namespace PCGVoxelLandscapes.Scripts;

/**
 * GUI node card for display of one saved world.
 * Actions open and delete.
 */
public partial class WorldSelectItem : PanelContainer
{
    private Label worldNameLabel;
    
    public override void _Ready()
    {
        worldNameLabel = GetNode<Label>("HBoxContainer/WorldNameLabel");
    }
    
    public void SetWorldName(string worldName)
    {
        worldNameLabel.Text = worldName;
    }
    
    /**
     * Sets up button press signal for open world button.
     */
    public void WireUpOnClickedOpen(Action<WorldSaveConfig> onClicked, WorldSaveConfig worldSaveConfig)
    {
        GetNode<Button>("HBoxContainer/OpenWorldButton").Pressed += () => onClicked(worldSaveConfig);
    }
    
    /**
     * Sets up button press signal for delete world button.
     */
    public void WireUpOnClickedDelete(Action<string> onClicked, string worldDirectoryName)
    {
        GetNode<Button>("HBoxContainer/DeleteWorldButton").Pressed += () => onClicked(worldDirectoryName);
    }
}
