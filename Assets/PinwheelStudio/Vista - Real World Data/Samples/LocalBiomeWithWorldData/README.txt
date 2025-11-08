This example showing how to:
- Download real world data using Data Provider Asset.
- Load data to the graph with LPB for further processing.
- Make use of the data, practicing & use cases.

DOWNLOADING DATA
1. Select the Demo_USGS_Colorado.asset file.
1.1. You can create yours using the Assets/Create/Vista/Data Provider menu
2. Click "Select region from Map" button to bring up the map view.
3. Click "Select a Region" in the map view
4. Draw a rectangle.
4.1. For USGS data provider, data only available in the US. 
     Sometimes it will return a "bad request" when downloading color map, it's a server side issue, not related to Vista. Please try again later or try another region.
4.2. For OpenTopography provider, data available for most area of the world.
5. Click "Done" and close the map view.
6. Click "Download" on the data provider asset.

PROCESSING, MAKE USE OF DATA, PRACTICING & USE CASES
1. Select the Local Procedural Biome in the scene (under the Vista Manager object)
2. Click the Edit button next to the terrain graph slot.
3. Continue learning in the graph editor.

IN THIS SCENE
An area of 20x20 kilometers Colorado mountainess shrinked into a grid of 2x2 kilometers game terrains (1/10 ratio).