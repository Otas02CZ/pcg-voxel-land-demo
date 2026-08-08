using Godot;
using PCGVoxelLandscapes.Scripts;

/**
 * Handles control of the in-game menu.
 */
public partial class InGame : Control
{

    private Label moveModeLabel;
    private Node2D crosshair;
    
    public override void _Ready()
    {
        moveModeLabel = GetNode<Label>("MoveMode/MoveModeLabel");
        crosshair = GetNode<Node2D>("Center/Crosshair");
    }

    /**
     * Sets up the component.
     */
    public void Setup(Player player)
    {
        SetMovementMode(player.GetMovementMode());
        player.SubscribeOnMovementModeChanged(SetMovementMode);
    }
    
    /**
     * Sets movement mode display label.
     */
    private void SetMovementMode(MovementMode mode)
    {
        moveModeLabel.Text = mode == MovementMode.FLY ? "Flying" : "Walking";
    }

    /**
     * Sets visibility of in-game crosshair.
     */
    public void SetCrosshairVisibility(bool visible)
    {
        crosshair.Visible = visible;
    }
}
