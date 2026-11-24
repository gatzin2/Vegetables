using System.IO;
using PlantAndDungeon.Prototype2D;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlantAndDungeon.Prototype2D.Editor
{
    [InitializeOnLoad]
    public static class BattlePrototypeBuilder
    {
        private const string Root = "Assets/Prototype2DCharacter";
        private const string Generated = Root + "/Generated";
        private const string Art = Generated + "/Art";
        private const string CharacterFolder = Root + "/Characters/Placeholder";
        private const string Animations = CharacterFolder + "/Animations";
        private const string ScenePath = Root + "/Scenes/BattleAnimationPrototype.unity";

        static BattlePrototypeBuilder()
        {
            EditorApplication.delayCall += BuildOnceWhenSafe;
        }

        [MenuItem("Tools/Plant and Dungeon/Fix Placeholder Attack And Feet %&#f")]
        public static void FixPlaceholderAttackAndFeet()
        {
            string path = CharacterFolder + "/PlaceholderFighter.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                // The boot sole is 0.47 units below the old rig origin.
                root.transform.Find("VisualRoot/Rig").localPosition = new Vector3(0, .47f, 0);
                Transform shadow = root.transform.Find("VisualRoot/ContactShadow");
                shadow.localPosition = Vector3.zero;
                shadow.localScale = new Vector3(1.4f, .2f, 1);
                Transform arm = root.transform.Find("VisualRoot/Rig/Hips/RightArmBone");
                arm.localRotation = Quaternion.Euler(0, 0, 55);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            AnimationClip attack = AssetDatabase.LoadAssetAtPath<AnimationClip>(Animations + "/Attack.anim");
            attack.ClearCurves();
            ConfigureAttackClip(attack);
            AnimationClip idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Animations + "/Idle.anim");
            idle.ClearCurves();
            ConfigureIdleClip(idle);
            EditorUtility.SetDirty(attack);
            EditorUtility.SetDirty(idle);
            AssetDatabase.SaveAssets();
            Debug.Log("[2D Battle Test Bench] Fixed horizontal attack and boot-sole origin on placeholder prefab.");
        }

        [MenuItem("Tools/Plant and Dungeon/Rebuild 2D Battle Prototype %&b")]
        public static void RebuildFromMenu()
        {
            Build(true);
        }

        [MenuItem("Tools/Plant and Dungeon/Validate Battle Test Bench %&v")]
        public static void ValidateTestBench()
        {
            string previous = SceneManager.GetActiveScene().path;
            if (SceneManager.GetActiveScene().isDirty) throw new System.InvalidOperationException("Save the active scene before validation.");
            try
            {
                EditorSceneManager.OpenScene(ScenePath);
                BattleAnimationTestBench bench = Object.FindObjectOfType<BattleAnimationTestBench>();
                if (bench == null || bench.PlayerPrefab == null || bench.EnemyPrefab == null ||
                    bench.PlayerInstance == null || bench.EnemyInstance == null ||
                    bench.PlayerAnchor == null || bench.EnemyAnchor == null)
                    throw new System.InvalidOperationException("Test bench references are incomplete.");
                Transform player = bench.PlayerInstance.transform;
                Transform enemy = bench.EnemyInstance.transform;
                Vector3 playerPosition = player.position;
                Vector3 enemyPosition = enemy.position;
                if (playerPosition.x >= enemyPosition.x || player.Find("VisualRoot").localScale.x <= 0 || enemy.Find("VisualRoot").localScale.x >= 0)
                    throw new System.InvalidOperationException("Characters must face inward from opposite sides.");
                AnimationClip idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Animations + "/Idle.anim");
                AnimationClip attack = AssetDatabase.LoadAssetAtPath<AnimationClip>(Animations + "/Attack.anim");
                idle.SampleAnimation(player.gameObject, 0f);
                idle.SampleAnimation(enemy.gameObject, 0f);
                ValidateFeet(player);
                ValidateFeet(enemy);
                CaptureTestBench("BattleTestBenchIdle.png");
                attack.SampleAnimation(player.gameObject, .36f);
                attack.SampleAnimation(enemy.gameObject, .36f);
                ValidateFeet(player);
                ValidateFeet(enemy);
                if (player.position != playerPosition || enemy.position != enemyPosition)
                    throw new System.InvalidOperationException("Attack moved a character off its anchor.");
                Transform playerWeapon = player.Find("VisualRoot/Rig/Hips/RightArmBone/Weapon");
                Transform enemyWeapon = enemy.Find("VisualRoot/Rig/Hips/RightArmBone/Weapon");
                if (playerWeapon.position.x <= playerPosition.x || enemyWeapon.position.x >= enemyPosition.x)
                    throw new System.InvalidOperationException("Attack must extend toward the opponent.");
                CaptureTestBench("BattleTestBenchAttack.png");
                Debug.Log("[2D Battle Test Bench] Validation passed: prefab references, anchors, inward facing, attack direction and fixed root positions.");
            }
            finally
            {
                EditorSceneManager.OpenScene(string.IsNullOrEmpty(previous) ? ScenePath : previous);
            }
        }

        private static void ValidateFeet(Transform character)
        {
            float sole = character.Find("VisualRoot/Rig/Hips/RightLegBone/RightBoot").GetComponent<SpriteRenderer>().bounds.min.y;
            float shadow = character.Find("VisualRoot/ContactShadow").position.y;
            if (Mathf.Abs(sole - shadow) > .08f)
                throw new System.InvalidOperationException("Contact shadow must stay at the boot sole during Idle and Attack.");
        }

        private static void CaptureTestBench(string filename)
        {
            Camera camera = Camera.main;
            RenderTexture render = RenderTexture.GetTemporary(1280, 720, 24);
            RenderTexture oldTarget = camera.targetTexture;
            RenderTexture oldActive = RenderTexture.active;
            Texture2D texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                texture.Apply();
                Directory.CreateDirectory("outputs");
                File.WriteAllBytes("outputs/" + filename, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                RenderTexture.ReleaseTemporary(render);
                Object.DestroyImmediate(texture);
            }
        }

        private static void BuildOnceWhenSafe()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || File.Exists(ScenePath))
                return;

            Scene current = SceneManager.GetActiveScene();
            if (current.isDirty || string.IsNullOrEmpty(current.path))
            {
                Debug.Log("[2D Battle Prototype] Auto-build skipped because the current scene has unsaved changes. Use Tools/Plant and Dungeon/Rebuild 2D Battle Prototype.");
                return;
            }

            Build(false);
        }

        private static void Build(bool force)
        {
            if (!force && File.Exists(ScenePath))
                return;

            EnsureFolders();
            string previousScene = SceneManager.GetActiveScene().path;
            if (force && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Existing configured benches and authored character assets survive rebuilds.
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (Object.FindObjectOfType<BattleAnimationTestBench>() != null)
                {
                    Debug.Log("[2D Battle Prototype] Test bench already configured; character resources and scene settings preserved.");
                    return;
                }
            }

            Sprite circle = CreateSprite("Circle", new Color32(222, 220, 196, 255), Shape.Circle);
            Sprite body = CreateSprite("Body", new Color32(80, 117, 105, 255), Shape.Rounded);
            Sprite dark = CreateSprite("Dark", new Color32(45, 48, 47, 255), Shape.Rounded);
            Sprite skin = CreateSprite("Skin", new Color32(222, 174, 130, 255), Shape.Circle);
            Sprite weapon = CreateSprite("Weapon", new Color32(194, 139, 70, 255), Shape.Rounded);
            Sprite dust = CreateSprite("Dust", new Color32(151, 119, 81, 255), Shape.Rounded);

            AnimationClip idle = CreateIdleClip();
            AnimationClip attack = CreateAttackClip();
            AnimationClip hit = CreateHitClip();
            AnimationClip death = CreateDeathClip();
            AnimatorController controller = CreateController(idle, attack, hit, death);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            CreateStage(circle, dust);
            string prefabPath = CharacterFolder + "/PlaceholderFighter.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                GameObject character = CreateCharacter("PlaceholderFighter", Vector3.zero, true, body, dark, skin, weapon, circle, controller, false);
                prefab = PrefabUtility.SaveAsPrefabAsset(character, prefabPath);
                Object.DestroyImmediate(character);
            }
            CreateTestBench(prefab.GetComponent<BattleCharacterView>());
            EditorSceneManager.SaveScene(scene, ScenePath);

            if (!string.IsNullOrEmpty(previousScene) && File.Exists(previousScene))
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
            else
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[2D Battle Prototype] Built scene at {ScenePath}");
        }

        private static void EnsureFolders()
        {
            EnsureFolder(Root, "Generated");
            EnsureFolder(Generated, "Art");
            EnsureFolder(Generated, "Animations");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "Characters");
            EnsureFolder(Root + "/Characters", "Placeholder");
            EnsureFolder(CharacterFolder, "Animations");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private enum Shape { Circle, Rounded }

        private static Sprite CreateSprite(string name, Color32 color, Shape shape)
        {
            string path = Art + "/" + name + ".png";
            if (!File.Exists(path))
            {
                const int size = 64;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color32 clear = new Color32(0, 0, 0, 0);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    bool inside = shape == Shape.Circle
                        ? nx * nx + ny * ny <= 0.92f
                        : Mathf.Abs(nx) <= 0.82f && Mathf.Abs(ny) <= 0.92f &&
                          (Mathf.Abs(nx) < 0.68f || Mathf.Abs(ny) < 0.78f ||
                           Mathf.Pow(Mathf.Abs(nx) - 0.68f, 2) + Mathf.Pow(Mathf.Abs(ny) - 0.78f, 2) < 0.02f);
                    texture.SetPixel(x, y, inside ? color : clear);
                }
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 64f;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void CreateCamera()
        {
            GameObject go = new GameObject("BattleCamera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 1.8f, -10f);
            Camera camera = go.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.backgroundColor = new Color(0.12f, 0.10f, 0.08f);
            camera.clearFlags = CameraClearFlags.SolidColor;
        }

        private static void CreateStage(Sprite circle, Sprite dust)
        {
            GameObject root = new GameObject("BattleStage", typeof(BattleStagePresenter));
            SpriteRenderer far = CreateLayer(root.transform, "FarBackground", dust, new Vector3(0, 1.8f, 2), new Vector3(22, 10, 1), new Color(0.36f, 0.25f, 0.18f), -30);
            SpriteRenderer mid = CreateLayer(root.transform, "TownSilhouette", dust, new Vector3(0, 0.8f, 1), new Vector3(18, 4.5f, 1), new Color(0.22f, 0.19f, 0.16f), -20);
            SpriteRenderer foreground = CreateLayer(root.transform, "BattleGround", circle, new Vector3(0, -2.2f, 0), new Vector3(18, 2.5f, 1), new Color(0.24f, 0.20f, 0.16f), -10);

            SerializedObject stage = new SerializedObject(root.GetComponent<BattleStagePresenter>());
            stage.FindProperty("farLayer").objectReferenceValue = far;
            stage.FindProperty("midLayer").objectReferenceValue = mid;
            stage.FindProperty("foregroundLayer").objectReferenceValue = foreground;
            stage.ApplyModifiedPropertiesWithoutUndo();

            GameObject title = new GameObject("Instructions", typeof(TextMesh));
            title.transform.SetParent(root.transform);
            title.transform.position = new Vector3(0, 4.2f, -0.5f);
            TextMesh text = title.GetComponent<TextMesh>();
            text.text = "FACE-TO-FACE 2D BATTLE TEST BENCH\nSelect BattleAnimationTestBench | Inspector: Play | Space Preview | 1 Reset";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.08f;
            text.fontSize = 48;
            text.color = new Color(0.93f, 0.86f, 0.72f);
        }

        private static SpriteRenderer CreateLayer(Transform parent, string name, Sprite sprite, Vector3 position, Vector3 scale, Color tint, int order)
        {
            GameObject go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = scale;
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static GameObject CreateCharacter(string characterName, Vector3 position, bool facesRight, Sprite body, Sprite dark, Sprite skin, Sprite weapon, Sprite circle, RuntimeAnimatorController controller, bool addInput)
        {
            GameObject root = addInput
                ? new GameObject(characterName, typeof(Animator), typeof(BattleCharacterView), typeof(BattlePrototypeInput))
                : new GameObject(characterName, typeof(Animator), typeof(BattleCharacterView));
            root.transform.position = position;
            root.GetComponent<Animator>().runtimeAnimatorController = controller;

            Transform visual = NewTransform("VisualRoot", root.transform, Vector3.zero);
            Transform rig = NewTransform("Rig", visual, Vector3.zero);
            rig.localPosition = new Vector3(0, .47f, 0);
            Transform hips = NewTransform("Hips", rig, new Vector3(0, 0.85f, 0));
            AddPart("Torso", hips, body, new Vector3(0, 0.55f, 0), new Vector3(0.68f, 1.3f, 1), 4);
            Transform head = NewTransform("HeadBone", hips, new Vector3(0.08f, 1.35f, 0));
            AddPart("Head", head, skin, Vector3.zero, new Vector3(0.68f, 0.82f, 1), 8);
            AddPart("Hair", head, dark, new Vector3(-0.12f, 0.2f, 0), new Vector3(0.65f, 0.52f, 1), 9);
            // Profile landmarks make +X unambiguously the authored facing direction.
            AddPart("Nose", head, skin, new Vector3(0.34f, -0.03f, 0), new Vector3(0.24f, 0.2f, 1), 10);
            AddPart("Eye", head, dark, new Vector3(0.21f, 0.06f, 0), new Vector3(0.075f, 0.09f, 1), 11);

            Transform leftArm = NewTransform("LeftArmBone", hips, new Vector3(-0.12f, 0.92f, 0));
            AddPart("LeftArm", leftArm, dark, new Vector3(0, -0.35f, 0), new Vector3(0.34f, 0.9f, 1), 2);
            Transform rightArm = NewTransform("RightArmBone", hips, new Vector3(0.18f, 0.92f, 0));
            rightArm.localRotation = Quaternion.Euler(0, 0, 55f);
            AddPart("RightArm", rightArm, dark, new Vector3(0, -0.35f, 0), new Vector3(0.34f, 0.9f, 1), 6);
            AddPart("Weapon", rightArm, weapon, new Vector3(0.12f, -0.95f, 0), new Vector3(0.18f, 1.5f, 1), 7);

            Transform leftLeg = NewTransform("LeftLegBone", hips, new Vector3(-0.28f, -0.02f, 0));
            AddPart("LeftLeg", leftLeg, dark, new Vector3(0, -0.7f, 0), new Vector3(0.42f, 1.25f, 1), 1);
            AddPart("LeftBoot", leftLeg, dark, new Vector3(0.12f, -1.2f, 0), new Vector3(0.62f, 0.22f, 1), 2);
            Transform rightLeg = NewTransform("RightLegBone", hips, new Vector3(0.28f, -0.02f, 0));
            AddPart("RightLeg", rightLeg, dark, new Vector3(0, -0.7f, 0), new Vector3(0.42f, 1.25f, 1), 3);
            AddPart("RightBoot", rightLeg, dark, new Vector3(0.12f, -1.2f, 0), new Vector3(0.62f, 0.22f, 1), 4);

            AddPart("ContactShadow", visual, circle, Vector3.zero, new Vector3(1.4f, .2f, 1), -1).color = new Color(0, 0, 0, 0.38f);

            SerializedObject view = new SerializedObject(root.GetComponent<BattleCharacterView>());
            view.FindProperty("animator").objectReferenceValue = root.GetComponent<Animator>();
            view.FindProperty("visualRoot").objectReferenceValue = visual;
            view.ApplyModifiedPropertiesWithoutUndo();
            root.GetComponent<BattleCharacterView>().SetFacing(facesRight);

            if (addInput)
            {
                SerializedObject input = new SerializedObject(root.GetComponent<BattlePrototypeInput>());
                input.FindProperty("character").objectReferenceValue = root.GetComponent<BattleCharacterView>();
                input.ApplyModifiedPropertiesWithoutUndo();
            }
            return root;
        }

        private static void CreateTestBench(BattleCharacterView prefab)
        {
            GameObject go = new GameObject("BattleAnimationTestBench", typeof(BattleAnimationTestBench));
            Transform player = NewTransform("PlayerAnchor", go.transform, new Vector3(-3.2f, -1.65f, 0));
            Transform enemy = NewTransform("EnemyAnchor", go.transform, new Vector3(3.2f, -1.65f, 0));
            SerializedObject data = new SerializedObject(go.GetComponent<BattleAnimationTestBench>());
            data.FindProperty("playerPrefab").objectReferenceValue = prefab;
            data.FindProperty("enemyPrefab").objectReferenceValue = prefab;
            data.FindProperty("playerAnchor").objectReferenceValue = player;
            data.FindProperty("enemyAnchor").objectReferenceValue = enemy;
            data.FindProperty("stage").objectReferenceValue = Object.FindObjectOfType<BattleStagePresenter>();
            data.ApplyModifiedPropertiesWithoutUndo();
            BattleAnimationTestBenchEditor.Assemble(go.GetComponent<BattleAnimationTestBench>());
        }

        private static Transform NewTransform(string name, Transform parent, Vector3 localPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static SpriteRenderer AddPart(string name, Transform parent, Sprite sprite, Vector3 localPosition, Vector3 localScale, int order)
        {
            GameObject go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static AnimationClip CreateIdleClip()
        {
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(Animations + "/Idle.anim");
            if (existing != null) return existing;
            AnimationClip clip = NewClip("Idle", true, 1.5f);
            ConfigureIdleClip(clip);
            SaveClip(clip);
            return clip;
        }

        private static AnimationClip CreateAttackClip()
        {
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(Animations + "/Attack.anim");
            if (existing != null) return existing;
            AnimationClip clip = NewClip("Attack", false, .65f);
            ConfigureAttackClip(clip);
            SaveClip(clip);
            return clip;
        }

        private static void ConfigureIdleClip(AnimationClip clip)
        {
            SetCurve(clip, "VisualRoot/Rig/Hips", "m_LocalPosition.y", Keys(0, .85f, 1.5f, .85f));
            SetCurve(clip, "VisualRoot/Rig/Hips/HeadBone", "localEulerAnglesRaw.z", Keys(0, -2, .75f, 2, 1.5f, -2));
            SetCurve(clip, "VisualRoot/Rig/Hips/RightArmBone", "localEulerAnglesRaw.z", Keys(0, 55, .75f, 58, 1.5f, 55));
        }

        private static void ConfigureAttackClip(AnimationClip clip)
        {
            // Positive Z rotates the downward-authored weapon toward +X; enemy mirrors it.
            SetCurve(clip, "VisualRoot/Rig/Hips/RightArmBone", "localEulerAnglesRaw.z", Keys(0, 55, .18f, 20, .32f, 90, .44f, 90, .65f, 55));
            SetCurve(clip, "VisualRoot/Rig/Hips/RightArmBone", "m_LocalPosition.x", Keys(0, .18f, .18f, .08f, .32f, .55f, .44f, .55f, .65f, .18f));
            SetCurve(clip, "VisualRoot/Rig/Hips/RightArmBone", "m_LocalPosition.y", Keys(0, .92f, .65f, .92f));
            SetCurve(clip, "VisualRoot/Rig/Hips", "m_LocalPosition.y", Keys(0, .85f, .65f, .85f));
        }

        private static AnimationClip CreateHitClip()
        {
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(Animations + "/Hit.anim");
            if (existing != null) return existing;
            AnimationClip clip = NewClip("Hit", false, .38f);
            SetCurve(clip, "VisualRoot/Rig", "m_LocalPosition.x", Keys(0, 0, .06f, -.35f, .14f, .18f, .24f, -.1f, .38f, 0));
            SetCurve(clip, "VisualRoot/Rig/Hips", "localEulerAnglesRaw.z", Keys(0, 0, .08f, 16, .38f, 0));
            SaveClip(clip);
            return clip;
        }

        private static AnimationClip CreateDeathClip()
        {
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(Animations + "/Death.anim");
            if (existing != null) return existing;
            AnimationClip clip = NewClip("Death", false, 1.1f);
            SetCurve(clip, "VisualRoot/Rig/Hips", "localEulerAnglesRaw.z", Keys(0, 0, .35f, -18, .85f, -88, 1.1f, -88));
            SetCurve(clip, "VisualRoot/Rig/Hips", "m_LocalPosition.y", Keys(0, .85f, .35f, .7f, .85f, .2f, 1.1f, .2f));
            SetCurve(clip, "VisualRoot/Rig/Hips", "m_LocalPosition.x", Keys(0, 0, .85f, -.65f, 1.1f, -.65f));
            SaveClip(clip);
            return clip;
        }

        private static AnimationClip NewClip(string name, bool loop, float duration)
        {
            AnimationClip clip = new AnimationClip { name = name, frameRate = 30f };
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.stopTime = duration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        private static AnimationCurve Keys(params float[] timeValuePairs)
        {
            Keyframe[] keys = new Keyframe[timeValuePairs.Length / 2];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = new Keyframe(timeValuePairs[i * 2], timeValuePairs[i * 2 + 1]);
            return new AnimationCurve(keys);
        }

        private static void SetCurve(AnimationClip clip, string path, string property, AnimationCurve curve)
        {
            // Preserve unanimated vector axes; SetCurve can bake omitted axes as zero.
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
        }

        private static void SaveClip(AnimationClip clip)
        {
            string path = Animations + "/" + clip.name + ".anim";
            AssetDatabase.CreateAsset(clip, path);
        }

        private static AnimatorController CreateController(AnimationClip idle, AnimationClip attack, AnimationClip hit, AnimationClip death)
        {
            string path = Animations + "/PrototypeHero.controller";
            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing != null) return existing;
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Reset", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idleState = machine.AddState("Idle"); idleState.motion = idle;
            AnimatorState attackState = machine.AddState("Attack"); attackState.motion = attack;
            AnimatorState hitState = machine.AddState("Hit"); hitState.motion = hit;
            AnimatorState deathState = machine.AddState("Death"); deathState.motion = death;
            machine.defaultState = idleState;

            AddCondition(machine.AddAnyStateTransition(attackState), AnimatorConditionMode.If, 0, "Attack", false, .04f);
            AddCondition(machine.AddAnyStateTransition(hitState), AnimatorConditionMode.If, 0, "Hit", false, .02f);
            AddCondition(machine.AddAnyStateTransition(deathState), AnimatorConditionMode.If, 0, "Die", false, .04f);
            AddCondition(machine.AddAnyStateTransition(idleState), AnimatorConditionMode.If, 0, "Reset", false, .05f);
            ConfigureExit(attackState.AddTransition(idleState), .88f, .08f);
            ConfigureExit(hitState.AddTransition(idleState), .88f, .05f);
            return controller;
        }

        private static void AddCondition(AnimatorStateTransition transition, AnimatorConditionMode mode, float threshold, string parameter, bool exitTime, float duration)
        {
            transition.hasExitTime = exitTime;
            transition.duration = duration;
            transition.AddCondition(mode, threshold, parameter);
        }

        private static void ConfigureExit(AnimatorStateTransition transition, float exitTime, float duration)
        {
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.duration = duration;
        }
    }
}
