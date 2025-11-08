It is recommended to take a look at the Demo_LocalBiomeWithWorldData scene first.

This example showing how to:
- Use the Real World Biome to create a clone of real world terrain without the need of caching data in asset.
- Load data to the graph for further processing.
- Make use of the data, do and don't.

IN THIS SCENE
An area of 40x40 kilometers mountain getting shrinked into a 2x2 kilometers grid of terrains (8x8 tiles) (1/20 ratio).
To reduce package size, texture resolution in the data providers and tiles settings are set to very low, you can increase them for better quality if you want. It will take longer to process.

UNITY TERRAIN WITH COLOR MAP?
Yes, terrains in this scene use Standard shader instead of Terrain Lit, that way we can display the downloaded color map.
Vista Terrain Tile don't support populating albedo map, so we created a custom component Terrain Color Map Populator to do that.
Color maps are populated as Render Texture, so they will be erased on some occasion.
Note: Draw Instancing on the terrains must be turned OFF for this to work.

REAL WORLD BIOME (RWB)
- Able to download data on the go, per-tile
- It has a boundary, defined by In Scene Width/Length property
- The region you selected in the map will be stretch to fill its in scene boundary -> It's up to you to make the proportion looks good.
- Data will be downloaded using the data provider asset under Data Providers section
- There is also a tiled image provider (Data_USGS_Color_Tiled) demostrating how to setup and use these thing, you can drop it to the Color Map Provider slot in the RWB inspector.

TRY ANOTHER LOCATION
- Select the RWB, click on "Select region from Map"
- Click on "Select a Region" button in the map view, draw a rectangle
- This example using USGS provider where data available only in the US
- Click "Done" and close the map.
- Regenerate the scene with Vista Manager

DATA PROCESSING, USAGE, DO AND DON'T
- Select the RWB, click on the "Edit" button next to the terrain graph slot.
- Continue learning in the graph editor.