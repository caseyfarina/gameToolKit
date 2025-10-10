# Unity 6 Blend Trees Implementation Guide

## Overview
Blend Trees in Unity 6 allow smooth blending between multiple similar animations using parameters from your scripts. This guide shows how to implement them with the educational physics controllers in this project.

## What Are Blend Trees?
Blend Trees combine multiple animations by incorporating parts of each animation at varying degrees. The blending is controlled by parameters (like `Speed` from our physics controllers) to create smooth transitions between different motion states.

## Prerequisites
- Animation clips (idle, walk, run animations)
- Animator Controller attached to your character
- Physics controller script setting animator parameters

## Step-by-Step Implementation

### 1. Create a Blend Tree State

1. **Open Animator Window**
   - Select your character with an Animator component
   - Window > Animation > Animator

2. **Create Blend Tree**
   - Right-click in empty space in Animator Controller
   - Select "Create State" > "From New Blend Tree"
   - Rename the state (e.g., "Movement")

3. **Enter Blend Tree Graph**
   - Double-click the Blend Tree state to enter the graph

### 2. Configure 1D Blend Tree (Most Common)

1. **Select Blend Tree Node**
   - Click on the root blend tree node
   - In Inspector, set "Blend Type" to "1D"

2. **Set Blend Parameter**
   - Choose parameter: `Speed` (matches our physics controllers)
   - This controls how animations blend based on movement velocity

3. **Add Animation Clips**
   - Click the `+` button in Motion field
   - Select "Add Motion Field"
   - Assign your animation clips:
     - **Threshold 0**: Idle animation
     - **Threshold 3**: Walk animation
     - **Threshold 8**: Run animation

4. **Adjust Thresholds**
   - Set threshold values based on your character's speed ranges
   - Example values match the `maxVelocity` settings in physics controllers

### 3. Parameter Setup

1. **Create Parameters**
   - In Animator window, click "Parameters" tab
   - Add Float parameter named `Speed`
   - This matches what our physics controllers send

2. **Physics Controller Integration**
   ```csharp
   // Our controllers already set this parameter:
   animator.SetFloat("Speed", horizontalVelocity.magnitude);
   ```

### 4. Advanced: 2D Blend Trees

For more complex movement (strafing, directional movement):

1. **Create 2D Blend Tree**
   - Set "Blend Type" to "2D Freeform Directional"
   - Add parameters: `SpeedX`, `SpeedZ`

2. **Add Directional Animations**
   - Forward, backward, left, right movement clips
   - Position them in 2D space representing movement directions

3. **Script Integration**
   ```csharp
   // Add to physics controller animation update:
   animator.SetFloat("SpeedX", rb.linearVelocity.x);
   animator.SetFloat("SpeedZ", rb.linearVelocity.z);
   ```

## Educational Implementation Examples

### Player Character Setup
1. Use our `PhysicsCharacterController` or `PhysicsPlayerController`
2. Create blend tree with idle/walk/run animations
3. Set thresholds: 0 (idle), 2 (walk), 6 (run)
4. Parameter automatically driven by `Speed` from controller

### Enemy Character Setup
1. Use our `PhysicsEnemyController`
2. Create blend tree with idle/patrol/chase animations
3. Use `IsChasing` boolean for chase/patrol states
4. Use `Speed` for movement intensity within each state

### Student Activity Ideas
1. **Blend Tree Experimentation**
   - Students adjust threshold values
   - Compare different animation clips
   - Observe how blending affects character feel

2. **Multi-State Setup**
   - Combine blend trees with state transitions
   - Use `IsGrounded` for jump/fall states
   - Connect to physics controller events

## Best Practices

### Animation Preparation
- **Similar Timing**: Use animations with similar durations
- **Matching Moments**: Align key poses (foot contacts) across clips
- **Root Motion**: Decide whether to use root motion or script-driven movement

### Threshold Values
- **Test and Adjust**: Start with default values, then fine-tune
- **Character-Specific**: Match thresholds to your physics settings
- **Visual Feedback**: Use Scene view to see blending in real-time

### Performance
- **Optimize Clips**: Remove unnecessary curves from animation clips
- **Layer Management**: Use Animator Layers for complex character systems
- **Parameter Efficiency**: Minimize parameter updates per frame

## Troubleshooting

### Common Issues
1. **Jerky Transitions**: Check animation alignment and threshold spacing
2. **No Blending**: Verify parameter names match script exactly
3. **Stuck in State**: Check transition conditions and parameter updates

### Debugging Tools
- **Animator Window**: Monitor parameter values in real-time
- **Animation Inspector**: Check clip properties and curves
- **Scene View**: Visual feedback while testing

## Integration with Event System

Our physics controllers include UnityEvents that can trigger animation states:

```csharp
// Connect events to animation triggers
onJump.AddListener(() => animator.SetTrigger("Jump"));
onLanding.AddListener(() => animator.SetTrigger("Land"));
onStartMoving.AddListener(() => animator.SetBool("IsMoving", true));
```

## Conclusion

Blend Trees provide smooth, professional-quality character animation while maintaining the educational event-driven approach. Students can experiment with different animation clips and threshold values to understand how parameter-driven animation creates responsive character movement.

The integration with our physics controllers makes this system plug-and-play for educational projects, allowing students to focus on animation principles rather than complex scripting.