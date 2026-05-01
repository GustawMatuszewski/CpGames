#!/usr/bin/env python3
"""Generate new wall material categories from source models in Buildings parts/Walls/"""

import os
import uuid
import shutil
import re

BASE_DIR = "/home/gustaw/CpGames/Assets/PlayerBuilding"
SOURCE_DIR = "/home/gustaw/CpGames/Assets/Buildings parts/Walls"

# Categories to create: (category_name, materialType_enum_value)
# materialType enum: none=0, wood=1, kindling=2, stone=3, metal=4, plastic=5, tissue=6, woolen=7, jeans=8, nylon=9, leather=10, tough=11
CATEGORIES = [
    ("Stone", 3),       # stone
    ("Wood", 1),        # wood
    ("Concrete", 4),    # metal (closest available)
    ("Metal", 4),       # metal
    ("Timber", 1),      # wood
    ("Rubble", 3),      # stone
    ("Sandstone", 3),   # stone
    ("Mud", 2),         # kindling (closest available)
    ("Clay", 2),        # kindling
    ("Hay", 2),         # kindling
]

# Mapping of variant type to list of source prefab name patterns
WALL_VARIANTS = {
    "Wall": [
        "wall 1", "wall 2", "wall 3",
        "wall.001", "wall.002", "wall.003", "wall.004", "wall.005",
        "wall.006", "wall.007", "wall.008", "wall.009", "wall.010",
        "wall.011", "wall.012", "wall.013", "wall.014", "wall.015",
    ],
    "WallCorner": [
        "wall corner outside", "wall corner inside",
        "wall corner 2x2",
    ],
    "WallFrame": [
        "frame",
    ],
    "WallHole": [
        "wall half",
    ],
    "WallWindow": [
        "wall window",
        "Double window",
        "Small Wall window",
        "Small Window",
        "Doors with wall",
    ],
}

def generate_guid():
    return uuid.uuid4().hex.upper()

def create_meta_file(path, guid, importer_type="ScriptImporter"):
    """Create a .meta file for a prefab or asset."""
    lines = [
        "fileFormatVersion: 2",
        f"guid: {guid}",
        f"{importer_type}:",
        "  externalObjects: {}",
        "  mainObjectFileID: 0",
        "  userData: ",
        "  assetBundleName: ",
        "  assetBundleVariant: ",
        "",
    ]
    with open(path, 'w') as f:
        f.write('\n'.join(lines))

def main():
    print("=== Generating Wall Categories ===")
    print(f"Source: {SOURCE_DIR}")
    print(f"Target: {BASE_DIR}")
    print()
    
    all_prefab_guids = {}  # category -> variant -> prefab_guid
    
    for category_name, material_type in CATEGORIES:
        print(f"Creating category: {category_name} (materialType={material_type})")
        
        # Create category directory
        cat_dir = os.path.join(BASE_DIR, category_name)
        os.makedirs(cat_dir, exist_ok=True)
        
        # Create .meta for the directory
        cat_meta_guid = generate_guid()
        meta_content = f"""fileFormatVersion: 2
guid: {cat_meta_guid}
FolderImporter:
  externalObjects: {{}}
  serializedVersion: 6
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
        with open(os.path.join(cat_dir, f"{category_name}.meta"), 'w') as f:
            f.write(meta_content)
        
        all_prefab_guids[category_name] = {}
        
        # Create each wall variant
        for variant_name, source_patterns in WALL_VARIANTS.items():
            full_name = f"{category_name}{variant_name}"
            asset_path = os.path.join(cat_dir, f"{full_name}.asset")
            prefab_path = os.path.join(cat_dir, f"{full_name}.prefab")
            
            # Find a matching source prefab
            source_prefab = None
            for pattern in source_patterns:
                source_prefab = find_source_prefab(pattern)
                if source_prefab:
                    break
            
            if source_prefab:
                # Copy the source prefab
                shutil.copy2(source_prefab, prefab_path)
                print(f"  ✓ {variant_name}: copied from {os.path.basename(source_prefab)}")
            else:
                print(f"  ✗ {variant_name}: no source found")
                continue
            
            # Generate GUID for this copied prefab
            prefab_guid = generate_guid()
            all_prefab_guids[category_name][variant_name] = prefab_guid
            
            # Create .meta for the prefab
            create_meta_file(prefab_path + ".meta", prefab_guid, "PrefabImporter")
            
            # Create .asset file directly at target location
            asset_meta_guid = generate_guid()
            content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 773518987f83ce650876c2337be76d44, type: 3}}
  m_Name: {full_name}
  m_EditorClassIdentifier: 
  itemID: 2137
  itemName: {full_name}
  icon: {{fileID: 0}}
  description: {category_name} wall
  useScript: {{fileID: 0}}
  craftingRecipe: {{fileID: 0}}
  weight: 0
  durability: 0
  usesLeft: 0
  burnCalories: 0
  itemType: 5
  materialType: {material_type}
  constructionPrefab: {{fileID: 6612603227374951970, guid: {prefab_guid}, type: 3}}
"""
            with open(asset_path, 'w') as f:
                f.write(content)
            
            # Create .meta for the asset
            create_meta_file(asset_path + ".meta", asset_meta_guid)
            
            print(f"  ✓ {full_name}.asset created (materialType={material_type})")
        
        print()
    
    print("=== Done! ===")
    print("Now open Unity to regenerate GUIDs for the imported prefabs.")
    print()
    print("Categories created:")
    for cat, variants in all_prefab_guids.items():
        print(f"  {cat}: {list(variants.keys())}")

def find_source_prefab(variant_name):
    """Find a source prefab matching the variant name pattern."""
    for root, dirs, files in os.walk(SOURCE_DIR):
        for f in files:
            if f.endswith('.prefab'):
                if variant_name.lower() in f.lower():
                    return os.path.join(root, f)
    return None

if __name__ == "__main__":
    main()
