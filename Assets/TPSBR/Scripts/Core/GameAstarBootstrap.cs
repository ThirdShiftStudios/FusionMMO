using Pathfinding;
using Pathfinding.Graphs.Grid;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPSBR
{
    /// <summary>
    /// Ensures the Game scene has an A* Pathfinding Project instance configured at runtime.
    /// </summary>
    public static class GameAstarBootstrap
    {
        private const string GameSceneName = "Game";
        private const float DefaultNodeSize = 1f;
        private const float DefaultGridExtent = 200f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            EnsureAstar(SceneManager.GetActiveScene());
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureAstar(scene);
        }

        private static void EnsureAstar(Scene scene)
        {
            if (scene.name != GameSceneName)
                return;

            if (Object.FindObjectOfType<AstarPath>() != null)
                return;

            GameObject container = new GameObject("A* Pathfinding");
            SceneManager.MoveGameObjectToScene(container, scene);

            AstarPath astarPath = container.AddComponent<AstarPath>();
            ConfigureGridGraph(astarPath);
            astarPath.Scan();
        }

        private static void ConfigureGridGraph(AstarPath astarPath)
        {
            GridGraph grid = astarPath.data.AddGraph(typeof(GridGraph)) as GridGraph;
            if (grid == null)
                return;

            SceneMap sceneMap = Object.FindObjectOfType<SceneMap>();
            Vector3 center = Vector3.zero;
            Vector2 size = new Vector2(DefaultGridExtent, DefaultGridExtent);

            if (sceneMap != null)
            {
                center = sceneMap.transform.position;
                Vector2Int worldDimensions = sceneMap.WorldDimensions;
                if (worldDimensions != Vector2Int.zero)
                {
                    size = new Vector2(worldDimensions.x, worldDimensions.y);
                }
            }

            int width = Mathf.Max(1, Mathf.CeilToInt(size.x / DefaultNodeSize));
            int depth = Mathf.Max(1, Mathf.CeilToInt(size.y / DefaultNodeSize));

            grid.center = center;
            grid.SetDimensions(width, depth, DefaultNodeSize);
            grid.maxSlope = 70f;
            grid.maxClimb = 1f;
            grid.collision.useRaycasting = true;
            grid.collision.mask = Physics.DefaultRaycastLayers;
            grid.collision.heightMask = Physics.DefaultRaycastLayers;
            grid.collision.diameter = 1f;
            grid.collision.height = 2f;
            grid.collision.use2D = false;
            grid.collision.type = GraphCollision.ColliderType.Capsule;
            grid.collision.thickRaycast = true;
        }
    }
}
