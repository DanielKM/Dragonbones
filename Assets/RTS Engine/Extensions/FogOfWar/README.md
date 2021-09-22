# RTS Engine Fog of War Extension

## Pipeline Setup

Follow the *UserGuide* of the Fog of War asset to set up the asset for Legacy Renderer, PPSv2, HDRP or URP.

For Legacy Renderer (example):

- Add the appropriate shaders to the Graphics setting as described in the *UserGuide* of the Fog of War asset.

## Scene Setup

For Legacy Renderer:

- Attach the FogOfWarLegacy component to the main camera object and other cameras you have in the map scene including the minimap one.

Create an empty game object as child of the GameManager and attach the *FogOfWarTeam* (from the FoW asset) *FogOfWarRTSManager* (from the extension) components to it and set the RTS Engine specific fields for the latter component:

- Fog Of War Cameras

For each entity, add the FogOfWarEntity component to it and update the RTS Engine specifics fields:

- Mininum Fog Strength
- Visible If Free
- Is Visible Post Discovery
- Same Visibility Objects

For buildings, you can add the *BuildingPlacerFogCondition* to the object that has the building's *BuildingPlacer* component to add a condition for placing buildings only in area with certain visibility that can be assigned from the inspector through:

- Minimum Fog Strength