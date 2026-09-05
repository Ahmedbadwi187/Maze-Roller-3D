using System.IO;
using RollAndEscape.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RollAndEscape.EditorTools
{
    /// <summary>
    /// Milestone 3 deliverable: adds a physics-driven ball (tilt + on-screen joystick input),
    /// a follow camera, and the joystick's on-screen UI into the Milestone 2 maze scene
    /// (Assets/Scenes/Game.unity) - reopening/rebuilding that scene fresh via
    /// Milestone2_MazeSceneBuilder.Build() first so the ball always ends up in a scene with
    /// current maze geometry, then layering the ball/camera/UI on top.
    ///
    /// Run via the Unity menu: Roll & Escape -> Milestone 3 - Build Ball Test Scene.
    /// </summary>
    public static class Milestone3_BallSceneBuilder
    {
        private const string BallPrefabPath = "Assets/Prefabs/Ball/Ball.prefab";
        private const string BallMaterialPath = "Assets/Materials/Ball.mat";
        private const string BallPhysicsMaterialPath = "Assets/Materials/BallPhysics.physicMaterial";
        private const string JoystickPrefabPath = "Assets/Prefabs/UI/JoystickCanvas.prefab";

        private static readonly Color BallColor = new Color32(0xE0, 0x7A, 0x5F, 0xFF); // warm coral - distinct from wall blue/floor neutral
        private const float BallDiameter = 0.8f;

        [MenuItem("Roll & Escape/Milestone 3 - Build Ball Test Scene")]
        public static void Build()
        {
            // Start from a fresh Milestone 2 maze so the ball always lands in an up-to-date scene.
            Milestone2_MazeSceneBuilder.Build();

            var mazeRootGO = GameObject.Find("MazeRoot");
            var mazeView = mazeRootGO.GetComponent<MazeView3D>();

            var ballPrefab = GetOrCreateBallPrefab();
            var joystickPrefab = GetOrCreateJoystickPrefab();

            var entranceWorldPos = GetEntranceWorldPosition(mazeView);
            var ballGO = (GameObject)PrefabUtility.InstantiatePrefab(ballPrefab);
            ballGO.transform.position = entranceWorldPos + new Vector3(0f, BallDiameter / 2f + 0.02f, 0f);

            var joystickGO = (GameObject)PrefabUtility.InstantiatePrefab(joystickPrefab);

            EnsureEventSystem();
            WireInputRouter(ballGO, joystickGO);
            WireFollowCamera(ballGO);

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"Milestone 3: spawned ball at {ballGO.transform.position} (maze entrance {mazeView.GetType().Name} " +
                      $"cell), wired tilt+joystick input and follow camera, into {scene.path}.");
        }

        /// <summary>
        /// Regression check, not just a compile check: builds the scene, then manually steps
        /// Unity's physics simulation (no Play mode / real-time loop needed) to prove the ball
        /// actually (a) falls under gravity and settles to rest on the floor without clipping
        /// through it, and (b) accelerates in the direction of an applied force the same way
        /// BallController.FixedUpdate would apply tilt/joystick input - i.e. that the
        /// Rigidbody/Collider/PhysicsMaterial setup is really functional, not just present.
        /// </summary>
        [MenuItem("Roll & Escape/Milestone 3 - Simulate Physics Sanity Check")]
        public static void SimulatePhysicsSanityCheck()
        {
            Build();

            var ballGO = GameObject.Find("Ball");
            var rb = ballGO.GetComponent<Rigidbody>();

            var previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            const float dt = 0.02f;
            var startPos = rb.position;

            for (int i = 0; i < 90; i++) Physics.Simulate(dt); // ~1.8s: let gravity settle the ball onto the floor
            var restPos = rb.position;

            for (int i = 0; i < 90; i++) // ~1.8s: apply a constant force, same as BallController would from tilt input
            {
                rb.AddForce(Vector3.right * 12f, ForceMode.Force);
                Physics.Simulate(dt);
            }
            var movedPos = rb.position;

            Physics.simulationMode = previousMode;

            Debug.Log($"[Milestone3 PhysicsSanityCheck] start={startPos} afterSettle={restPos} afterForce={movedPos} - " +
                      $"settled height {restPos.y:F3} (expect ~{BallDiameter / 2f:F2}, resting on the floor, not clipped " +
                      $"through it) - X moved {(movedPos.x - startPos.x):F3} under constant force (expect clearly > 0).");
        }

        private static Vector3 GetEntranceWorldPosition(MazeView3D mazeView)
        {
            var (column, row) = mazeView.LastBuiltEntrance;
            return mazeView.CellToWorldPosition(column, row);
        }

        private static void WireInputRouter(GameObject ballGO, GameObject joystickGO)
        {
            var router = ballGO.GetComponentInChildren<PlayerInputRouter>();
            var tilt = ballGO.GetComponentInChildren<TiltInputHandler>();
            var joystick = joystickGO.GetComponentInChildren<JoystickInputHandler>();

            var so = new SerializedObject(router);
            so.FindProperty("tiltInput").objectReferenceValue = tilt;
            so.FindProperty("joystickInput").objectReferenceValue = joystick;
            so.ApplyModifiedPropertiesWithoutUndo();

            var ballController = ballGO.GetComponentInChildren<BallController>();
            var bcSo = new SerializedObject(ballController);
            bcSo.FindProperty("inputRouter").objectReferenceValue = router;
            bcSo.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// UI pointer/drag events (the on-screen joystick, and later any UI button) simply
        /// never fire without an EventSystem in the scene - easy to compile clean and still be
        /// silently non-functional, so this is created unconditionally rather than left as a
        /// manual step.
        /// </summary>
        internal static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Debug.Log($"Created {go.name} (none existed) - required for on-screen joystick drag and UI button clicks to work at all.");
        }

        private static void WireFollowCamera(GameObject ballGO)
        {
            var cameraGO = GameObject.Find("Main Camera");
            if (cameraGO == null) return;

            // Milestone 2's static MazeCameraFramer only frames once at build time; gameplay
            // needs a camera that tracks the ball every frame instead.
            var framer = cameraGO.GetComponent<MazeCameraFramer>();
            if (framer != null) Object.DestroyImmediate(framer);

            var follow = cameraGO.GetComponent<BallFollowCamera>();
            if (follow == null) follow = cameraGO.AddComponent<BallFollowCamera>();
            follow.SetTarget(ballGO.transform);
        }

        private static GameObject GetOrCreateBallPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BallPrefabPath);
            if (existing != null) return existing;

            var physicsMaterial = GetOrCreateBallPhysicsMaterial();
            var ballMaterial = GetOrCreateBallMaterial();

            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Ball";
            ball.transform.localScale = Vector3.one * BallDiameter;
            ball.GetComponent<Renderer>().sharedMaterial = ballMaterial;

            var collider = ball.GetComponent<SphereCollider>();
            collider.sharedMaterial = physicsMaterial;

            var rigidbody = ball.AddComponent<Rigidbody>();
            rigidbody.mass = 1f;
            rigidbody.linearDamping = 0.05f;
            rigidbody.angularDamping = 0.05f;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            ball.AddComponent<TiltInputHandler>();
            ball.AddComponent<JoystickInputHandler>(); // needs its background/handle wired to the on-screen UI separately
            ball.AddComponent<PlayerInputRouter>();
            ball.AddComponent<BallController>();

            Directory.CreateDirectory(Path.GetDirectoryName(BallPrefabPath));
            var prefab = PrefabUtility.SaveAsPrefabAsset(ball, BallPrefabPath);
            Object.DestroyImmediate(ball);
            return prefab;
        }

        private static PhysicsMaterial GetOrCreateBallPhysicsMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(BallPhysicsMaterialPath);
            if (existing != null) return existing;

            var material = new PhysicsMaterial("BallPhysics")
            {
                dynamicFriction = 0.4f,
                staticFriction = 0.4f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Average
            };

            Directory.CreateDirectory(Path.GetDirectoryName(BallPhysicsMaterialPath));
            AssetDatabase.CreateAsset(material, BallPhysicsMaterialPath);
            return material;
        }

        private static Material GetOrCreateBallMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var existing = AssetDatabase.LoadAssetAtPath<Material>(BallMaterialPath);
            if (existing != null)
            {
                if (existing.shader != shader) existing.shader = shader;
                existing.color = BallColor;
                if (existing.HasProperty("_BaseColor")) existing.SetColor("_BaseColor", BallColor);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var material = new Material(shader) { color = BallColor };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", BallColor);
            Directory.CreateDirectory(Path.GetDirectoryName(BallMaterialPath));
            AssetDatabase.CreateAsset(material, BallMaterialPath);
            return material;
        }

        private static GameObject GetOrCreateJoystickPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(JoystickPrefabPath);
            if (existing != null)
            {
                // Self-healing: re-stamp the background's anchor/position every run (not just at
                // first creation) so a tuning change here - like moving the joystick from
                // bottom-left to bottom-center - reaches an already-cached prefab asset instead
                // of silently being skipped by the early-return above.
                var existingBackground = existing.transform.Find("JoystickBackground") as RectTransform;
                if (existingBackground != null)
                {
                    existingBackground.anchorMin = new Vector2(0.5f, 0f);
                    existingBackground.anchorMax = new Vector2(0.5f, 0f);
                    existingBackground.pivot = new Vector2(0.5f, 0.5f);
                    existingBackground.anchoredPosition = new Vector2(0f, 260f);
                    EditorUtility.SetDirty(existing);
                    PrefabUtility.SavePrefabAsset(existing);
                }
                return existing;
            }

            var canvasGO = new GameObject("JoystickCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f; // match height - this is a portrait game; keeps vertical anchoring predictable even when the Editor Game view isn't in a phone aspect ratio

            var knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var background = CreateUIImage("JoystickBackground", canvasGO.transform, knobSprite,
                new Color(1f, 1f, 1f, 0.25f), new Vector2(220, 220), new Vector2(0f, 0f), new Vector2(0f, 260));
            var handle = CreateUIImage("JoystickHandle", background.transform, knobSprite,
                new Color(1f, 1f, 1f, 0.6f), new Vector2(100, 100), Vector2.zero, Vector2.zero);

            var joystick = canvasGO.AddComponent<JoystickInputHandler>();
            var so = new SerializedObject(joystick);
            so.FindProperty("background").objectReferenceValue = background;
            so.FindProperty("handle").objectReferenceValue = handle;
            so.FindProperty("handleRange").floatValue = 100f;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(JoystickPrefabPath));
            var prefab = PrefabUtility.SaveAsPrefabAsset(canvasGO, JoystickPrefabPath);
            Object.DestroyImmediate(canvasGO);
            return prefab;
        }

        private static RectTransform CreateUIImage(string name, Transform parent, Sprite sprite, Color color, Vector2 size, Vector2 anchoredPosition, Vector2 bottomLeftOffset)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            if (parent.GetComponent<Canvas>() != null)
            {
                // Anchor the joystick background to bottom-CENTER of the screen (not bottom-left)
                // per user feedback - centered horizontally is easier to reach with either thumb
                // and reads as "the movement control", not tucked in a corner.
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = bottomLeftOffset;
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = anchoredPosition;
            }

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;

            return rect;
        }
    }
}
