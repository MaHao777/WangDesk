# Fix Dog Animation Leg Detachment

## Problem
During the running animation, the dog's legs appear to detach from the body. This is because the current implementation calculates leg positions using a linear approximation (`Sin(angle) * distance`) for the vertical offset caused by body rotation. This method is inaccurate when the body rotates around a pivot point, especially combined with vertical bouncing.

## Proposed Solution
Implement a "bone binding" mechanism using coordinate transformation matrices.

1.  **Define Rigging Points**: Establish fixed "attachment points" on the body for the Hip, Shoulder, Neck, and Tail relative to the body's center.
2.  **Coordinate Transformation**: 
    - Create a `RotateTransform` object that matches the body's current tilt angle and pivot point.
    - Use `transform.Transform(Point)` to calculate the exact screen coordinates of each attachment point for every frame.
3.  **Draw Limbs at Exact Coordinates**:
    - Draw the back legs at the transformed **Hip** position.
    - Draw the front legs at the transformed **Shoulder** position.
    - Draw the head at the transformed **Neck** position.
    - Draw the tail at the transformed **Tail** position.

## Implementation Details
- **File**: `src/WangDesk.App/Animations/PetAnimationGenerator.cs`
- **Method**: `DrawRunningTeddy`
- **Logic Change**: 
    - Remove `backLegTiltOffset` and `frontLegTiltOffset` calculations.
    - Introduce `bodyTransform` and point transformation logic.
    - Update `DrawFluffyBall` calls to use the new transformed coordinates.

## Verification
- Rebuild the project.
- Visual inspection of the running animation to ensure limbs stay attached to the body at all tilt angles.
