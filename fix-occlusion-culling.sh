#!/bin/bash
# Fix for Unity Scene View occlusion culling hiding buildings
# This script clears the baked occlusion culling data from the GrayBoxedMap scene
#
# IMPORTANT: After running this, you MUST also:
#   1. Open Unity and open the GrayBoxedMap scene
#   2. In the Scene View toolbar (top-left), click the "Occlusion Culling" button to toggle it OFF
#   3. If the Occlusion Culling window is open, switch from Visualization tab to Bake tab
#   4. Then re-bake if you still want occlusion culling

PROJECT="/home/gustaw/CpGames"
SCENE_DIR="$PROJECT/Assets/Scenes/GrayBoxedMap"
SCENE_FILE="$PROJECT/Assets/Scenes/GrayBoxedMap.unity"

echo "=== Fixing Occlusion Culling Issue ==="
echo ""

# Step 1: Remove baked occlusion culling data
echo "[1/4] Removing baked occlusion culling data..."
if [ -f "$SCENE_DIR/OcclusionCullingData.asset" ]; then
    rm "$SCENE_DIR/OcclusionCullingData.asset"
    echo "  -> Deleted OcclusionCullingData.asset"
else
    echo "  -> No baked data found (already clean)"
fi

# Step 2: Remove meta file
if [ -f "$SCENE_DIR/OcclusionCullingData.asset.meta" ]; then
    rm "$SCENE_DIR/OcclusionCullingData.asset.meta"
    echo "  -> Deleted OcclusionCullingData.asset.meta"
fi

# Step 3: Remove baking set
if [ -f "$SCENE_DIR/GrayBoxedMap Baking Set.asset" ]; then
    rm "$SCENE_DIR/GrayBoxedMap Baking Set.asset"
    echo "  -> Deleted GrayBoxedMap Baking Set.asset"
fi
if [ -f "$SCENE_DIR/GrayBoxedMap Baking Set.asset.meta" ]; then
    rm "$SCENE_DIR/GrayBoxedMap Baking Set.asset.meta"
    echo "  -> Deleted GrayBoxedMap Baking Set.asset.meta"
fi

# Step 4: Clear the OcclusionCullingSettings reference in the scene file
echo "[2/4] Clearing occlusion culling settings from scene file..."
if [ -f "$SCENE_FILE" ]; then
    # Check if the scene has occlusion culling settings
    if grep -q "m_OcclusionCullingData:" "$SCENE_FILE"; then
        # Replace the OcclusionCullingSettings block with an empty one
        sed -i 's/  m_OcclusionCullingData: {fileID: [0-9]*, guid: [a-f0-9]*, type: 2}/  m_OcclusionCullingData: {fileID: 0}/' "$SCENE_FILE"
        echo "  -> Cleared occlusion culling data reference in scene"
    else
        echo "  -> No occlusion culling data reference found (already clean)"
    fi
else
    echo "  -> ERROR: Scene file not found at $SCENE_FILE"
    exit 1
fi

echo ""
echo "=== Script Fix Complete ==="
echo ""
echo "NEXT STEPS (MUST DO IN UNITY EDITOR):"
echo "======================================="
echo ""
echo "1. Open Unity and load the GrayBoxedMap scene"
echo ""
echo "2. FIX THE SCENE VIEW (TOP-LEFT TOOLBAR):"
echo "   Look at the Scene View toolbar (top-left corner, near the camera icon)"
echo "   Click the button labeled 'Occlusion Culling' to toggle it OFF"
echo "   (It should NOT be highlighted/active)"
echo ""
echo "3. CHECK OCCLUSION CULLING WINDOW:"
echo "   If Window > Rendering > Occlusion Culling is open:"
echo "   - Switch from 'Visualization' tab to 'Bake' tab"
echo ""
echo "4. (OPTIONAL) Re-bake occlusion culling if needed:"
echo "   - Mark your buildings as 'Occludee Static'"
echo "   - Mark walls as 'Occluder Static' + 'Occludee Static'"
echo "   - Window > Rendering > Occlusion Culling > Bake tab > Bake"
echo ""
echo "Your buildings should now be visible in the Scene View! desu~ (◕‿◕) ★"
