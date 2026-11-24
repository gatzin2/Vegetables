using PlantAndDungeon.Prototype2D;
using UnityEditor;
using UnityEngine;

namespace PlantAndDungeon.Prototype2D.Editor
{
    [CustomEditor(typeof(BattleAnimationTestBench))]
    public sealed class BattleAnimationTestBenchEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("角色资源与固定站位", EditorStyles.boldLabel);
            PrefabField("playerPrefab", "我方角色 Prefab");
            PrefabField("enemyPrefab", "敌方角色 Prefab");
            Field("playerAnchor", "我方站位点（朝右）");
            Field("enemyAnchor", "敌方站位点（朝左）");
            EditorGUILayout.Space();
            Field("stage", "战斗舞台");
            Field("theme", "战斗背景主题");
            EditorGUILayout.Space();
            Field("previewTarget", "预览对象");
            Field("previewAction", "预览动作");
            Field("customStateName", "自定义技能状态名", "留空使用预览动作；填写 Animator 第 0 层完整状态名，例如 Base Layer.Cast。");
            serializedObject.ApplyModifiedProperties();

            BattleAnimationTestBench bench = (BattleAnimationTestBench)target;
            if (bench.PlayerPrefab == null || bench.EnemyPrefab == null || bench.PlayerAnchor == null || bench.EnemyAnchor == null)
                EditorGUILayout.HelpBox("请指定双方角色 Prefab 与站位点。角色 Prefab 根节点需要 BattleCharacterView。", MessageType.Warning);
            EditorGUILayout.HelpBox("编辑模式点击装配，预览双方外观。进入 Play Mode 后播放动作。自定义技能使用角色自己的 Animator 状态。", MessageType.Info);

            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("装配／替换双方角色")) Assemble(bench);
            if (GUILayout.Button("应用背景主题"))
            {
                BattleStagePresenter stage = (BattleStagePresenter)serializedObject.FindProperty("stage").objectReferenceValue;
                if (stage != null) Undo.RecordObjects(stage.GetComponentsInChildren<SpriteRenderer>(), "Apply battle theme");
                bench.ApplyTheme();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(bench.gameObject.scene);
            }
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("播放所选动作（空格）")) bench.PlayPreview();
                if (GUILayout.Button("双方复位／恢复待机（1）")) bench.ResetPreview();
            }
        }

        private void Field(string name, string label, string tooltip = null)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(name), new GUIContent(label, tooltip));
        }

        private void PrefabField(string name, string label)
        {
            SerializedProperty property = serializedObject.FindProperty(name);
            BattleCharacterView selected = (BattleCharacterView)EditorGUILayout.ObjectField(label,
                property.objectReferenceValue, typeof(BattleCharacterView), false);
            if (selected == null || (PrefabUtility.IsPartOfPrefabAsset(selected) && selected.transform.parent == null))
                property.objectReferenceValue = selected;
            else
                EditorGUILayout.HelpBox("请选择根节点挂有 BattleCharacterView 的角色 Prefab。", MessageType.Warning);
        }

        public static void Assemble(BattleAnimationTestBench bench)
        {
            if (Application.isPlaying) return;
            SerializedObject data = new SerializedObject(bench);
            Replace(data, "playerInstance", bench.PlayerInstance, bench.PlayerPrefab, bench.PlayerAnchor, true);
            Replace(data, "enemyInstance", bench.EnemyInstance, bench.EnemyPrefab, bench.EnemyAnchor, false);
            data.ApplyModifiedProperties();
        }

        private static void Replace(SerializedObject data, string field, BattleCharacterView oldView, BattleCharacterView prefab, Transform anchor, bool right)
        {
            if (prefab == null || anchor == null || !PrefabUtility.IsPartOfPrefabAsset(prefab)) return;
            if (oldView != null) Undo.DestroyObjectImmediate(oldView.gameObject);
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, anchor);
            Undo.RegisterCreatedObjectUndo(go, "Assemble battle character");
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            BattleCharacterView view = go.GetComponent<BattleCharacterView>();
            view.SetFacing(right);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
            foreach (Transform child in go.GetComponentsInChildren<Transform>())
                PrefabUtility.RecordPrefabInstancePropertyModifications(child);
            data.FindProperty(field).objectReferenceValue = view;
        }
    }
}
