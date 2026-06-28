// FILE: Player.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Player management and control script.

using System;
using Godot;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Types of player / camera movement.
 */
public enum MovementMode : byte
{
	FLY,
	WALK
}

/**
 * Player management and control script.
 */
public partial class Player : CharacterBody3D
{
	[Export] private float movementSpeed { get; set; } = 2.5f;
	[Export] private float mouseSensitivity { get; set; } = 0.003f;
	[Export] private float sprintMultiplier { get; set; } = 4.0f;
	[Export] private float jumpVelocity { get; set; } = 4.5f;
	[Export] private MovementMode movementMode;

    [Export] private float minVerticalRotation { get; set; } = -1.5f;
    [Export] private float maxVerticalRotation { get; set; } = 1.5f;

    private Action OnPlayerPositionChanged;

	private Camera3D camera;
    private OmniLight3D torch;
    private Vector3 velocity = Vector3.Zero;
    private float rotationHorizontal;
    private float rotationVertical;
    private bool mouseModeCaptured = true;

    // currently applied origin shift offset
    private Vector3Int originShiftOffsetXZ;
    
    // player state variables
    private bool controlsEnabled;
    private bool originShiftControlsChangeEnabled;
    private MovementMode previousMovementMode;
    
    // world boundary limits
    private int worldLimitMetersXMin;
    private int worldLimitMetersXMax;
    private int worldLimitMetersZMin;
    private int worldLimitMetersZMax;
    private int worldLimitMetersYMin;
    private int worldLimitMetersYMax;
    
    private float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    /**
     * Initialize player node.
     */
    public override void _Ready()
    {
        base._Ready();
        movementMode = MovementMode.FLY;
        camera = GetNode<Camera3D>("Camera");
        torch = GetNode<OmniLight3D>("Torch");
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    /**
     * Processes mouse motion.
     */
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!controlsEnabled)
            return;
        
        if (@event is InputEventMouseMotion mouseMotion)
        {
            rotationHorizontal -= mouseMotion.Relative.X * mouseSensitivity;
            rotationVertical -= mouseMotion.Relative.Y * mouseSensitivity;
            // avoid full upside-down flip
            rotationVertical = Mathf.Clamp(rotationVertical, minVerticalRotation, maxVerticalRotation);
            // apply to player
            Rotation = new Vector3(rotationVertical, rotationHorizontal, 0);
        }
    }

    /**
     * Process user input mostly related to movement.
     */
    public override void _PhysicsProcess(double delta)
    {
        if (!controlsEnabled)
            return;
        
        var lastPosition = Position;

        if (Input.IsActionJustPressed("toggle_torch"))
        {
            torch.SetVisible(!torch.Visible);
        }
        
        if (Input.IsActionJustPressed("toggle_movement"))
        {
            ToggleMovementMode();
        }
        
        if (movementMode == MovementMode.FLY)
        {
            HandleFlying(delta);
        }
        else
        {
            HandleWalking(delta);
        }
        
        // emit signal when position changes
        if (!Position.IsEqualApprox(lastPosition))
        {
            OnPlayerPositionChanged?.Invoke();
        }
    }

    /**
     * Handles player movement in flying mode.
     */
    private void HandleFlying(double delta)
    {
        Vector3 inputDir = Vector3.Zero;
        
        // assemble direction vector from user input
        if (Input.IsActionPressed("move_forward"))
            inputDir -= Transform.Basis.Z;
        if (Input.IsActionPressed("move_back"))
            inputDir += Transform.Basis.Z;
        if (Input.IsActionPressed("move_left"))
            inputDir -= Transform.Basis.X;
        if (Input.IsActionPressed("move_right"))
            inputDir += Transform.Basis.X;
        if (Input.IsActionPressed("move_up"))
            inputDir += Transform.Basis.Y;
        if (Input.IsActionPressed("move_down"))
            inputDir -= Transform.Basis.Y;

        inputDir = inputDir.Normalized();

        // apply sprint if pressed
        float speed = movementSpeed;
        if (Input.IsActionPressed("sprint"))
            speed *= sprintMultiplier;

        // change position based on movement direction and speed
        Position += inputDir * speed * (float)delta;
        
        // clamp position within world limits
        ClampPositionWithinWorldLimits();
    }

    /**
     * Handles player movement in walking mode. Player moves on collision surfaces with MoveAndSlide.
     */
    private void HandleWalking(double delta)
    {
        // gravity
        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;

        // jumping only when on the floor
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
            velocity.Y = jumpVelocity;

        // input direction
        Vector3 inputDir = Vector3.Zero;
        if (Input.IsActionPressed("move_forward"))
            inputDir -= Transform.Basis.Z;
        if (Input.IsActionPressed("move_back"))
            inputDir += Transform.Basis.Z;
        if (Input.IsActionPressed("move_left"))
            inputDir -= Transform.Basis.X;
        if (Input.IsActionPressed("move_right"))
            inputDir += Transform.Basis.X;
        
        Vector3 direction = inputDir.Normalized();

        // apply sprint if pressed
        float speed = movementSpeed;
        if (Input.IsActionPressed("sprint"))
            speed *= sprintMultiplier;

        // update velocity
        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * speed;
            velocity.Z = direction.Z * speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, speed);
        }

        // move on the surface
        Velocity = velocity;
        MoveAndSlide();
        
        // clamp position within world limits
        ClampPositionWithinWorldLimits();
    }
    
    /**
     * Ensures player stays within defined boundary of the world.
     */
    private void ClampPositionWithinWorldLimits()
    {
        Vector3Double realPosition = GetRealPosition();
        realPosition.x = Math.Clamp(realPosition.x, worldLimitMetersXMin, worldLimitMetersXMax);
        realPosition.y = Math.Clamp(realPosition.y, worldLimitMetersYMin, worldLimitMetersYMax);
        realPosition.z = Math.Clamp(realPosition.z, worldLimitMetersZMin, worldLimitMetersZMax);
        Position = new Vector3((float)(realPosition.x + originShiftOffsetXZ.x), (float)realPosition.y, (float)(realPosition.z + originShiftOffsetXZ.z));
    }
    
    /**
     * Toggle movement mode between flying and walking.
     */
    private void ToggleMovementMode()
    {
        if (movementMode == MovementMode.FLY)
        {
            movementMode = MovementMode.WALK;
            velocity = Vector3.Zero;
            GD.Print("Movement mode: WALK");
        }
        else
        {
            movementMode = MovementMode.FLY;
            velocity = Vector3.Zero;
            GD.Print("Movement mode: FLY");
        }
    }

    /**
     * Returns direction vector, where camera is looking
     */
    public Vector3 GetLookAtDirection()
    {
        Vector3 forward = -Transform.Basis.Z;
        return forward.Normalized();
    }
    
    /**
     * Returns real world position of camera.
     * Necessary to invert application of origin shift offset.
     */
    public Vector3Double GetCenteredCameraRealPosition()
    {
        Vector3 cameraPosition = camera.GlobalTransform.Origin;
        return new Vector3Double(cameraPosition.X - originShiftOffsetXZ.x, cameraPosition.Y, cameraPosition.Z - originShiftOffsetXZ.z);
    }
    
    /**
     * Returns real position of player.
     * Necessary to invert application of origin shift offset.
     */
    public Vector3Double GetRealPosition()
    {
        return new Vector3Double((double)Position.X - originShiftOffsetXZ.x, Position.Y, (double)Position.Z - originShiftOffsetXZ.z);
    }
    
    /**
     * Stops player controls and movement before origin shift is performed.
     */
    public void FreezeBeforeOriginShift()
    {
        if (originShiftControlsChangeEnabled)
        {
            controlsEnabled = false;
            previousMovementMode = movementMode;
            movementMode = MovementMode.FLY;
            Velocity = Vector3.Zero;
        }
    }
    
    /**
     * Resumes player controls and movement after origin shift.
     */
    public void UnfreezeAfterOriginShift()
    {
        if (originShiftControlsChangeEnabled)
        {
            controlsEnabled = true;
            movementMode = previousMovementMode;
        }
    }

    /**
     * Applies origin shift with newly specified offset.
     * Actual real position of player is obtained, shifted and player is placed at the new position.
     */
    public void ApplyOriginShift(Vector3Int newOriginShiftOffsetXZ)
    {
        // calculate real position
        Vector3Double realPosition = GetRealPosition();
        // apply new origin shift
        originShiftOffsetXZ = newOriginShiftOffsetXZ;
        // set new position
        Position = new Vector3((float)(realPosition.x + originShiftOffsetXZ.x), (float)realPosition.y, (float)(realPosition.z + originShiftOffsetXZ.z));
    }

    /**
     * Resets player settings to default mode at world startup.
     */
    public void ResetPlayerSettings()
    {
        movementMode = MovementMode.FLY;
        velocity = Vector3.Zero;
        rotationHorizontal = 0f;
        rotationVertical = 0f;
        torch.SetVisible(false);
    }
    
    /**
     * Disables controls and pause/resume steps of origin shift.
     */
    public void DisableControls()
    {
        controlsEnabled = false;
        originShiftControlsChangeEnabled = false;
    }

    /**
     * Enables controls and pause/resume steps of origin shift.
     */
    public void EnableControls()
    {
        controlsEnabled = true;
        originShiftControlsChangeEnabled = true;
    }
    
    /**
     * Encapsulated TeleportPlayer call with builtin data types for use in call deferred.
     */
    private void TeleportPlayer(double x, double y, double z)
    {
        Vector3Double realPosition = new Vector3Double(x, y, z);
        TeleportPlayer(realPosition);
    }

    /**
     * Teleports player to given real world position.
     * Resets origin shift.
     */
    public void TeleportPlayer(Vector3Double realPosition)
    {
        originShiftOffsetXZ = new Vector3Int(0, 0, 0);
        Position = new Vector3((float)realPosition.x, (float)realPosition.y, (float)realPosition.z);
        Velocity = Vector3.Zero;
        
        // clamp position within world limits
        Position = new Vector3(
            Mathf.Clamp(Position.X, worldLimitMetersXMin, worldLimitMetersXMax),
            Mathf.Clamp(Position.Y, worldLimitMetersYMin, worldLimitMetersYMax),
            Mathf.Clamp(Position.Z, worldLimitMetersZMin, worldLimitMetersZMax)
        );
    }
    
    /**
     * Configure world limit values.
     */
    public void SetLimits(int worldLimitMetersMinX, int worldLimitMetersMaxX, int worldLimitMetersMinZ, int worldLimitMetersMaxZ, int worldLimitMetersMinY, int worldLimitMetersMaxY)
    {
        worldLimitMetersXMin = worldLimitMetersMinX;
        worldLimitMetersXMax = worldLimitMetersMaxX;
        worldLimitMetersZMin = worldLimitMetersMinZ;
        worldLimitMetersZMax = worldLimitMetersMaxZ;
        worldLimitMetersYMin = worldLimitMetersMinY;
        worldLimitMetersYMax = worldLimitMetersMaxY;
    }
    
    public OmniLight3D GetTorch()
    {
        return torch;
    }

    /**
     * Subscribes supplied function to player position changed signal.
     */
    public void SubscribeOnPlayerPositionChanged(Action handler)
    {
        OnPlayerPositionChanged += handler;
    }
}
