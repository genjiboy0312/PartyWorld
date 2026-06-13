using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 씬 참조를 에디터에서 드래그 앤 드롭으로 설정할 수 있게 하는 래퍼
/// </summary>
[Serializable]
public class SceneReference
{
    [SerializeField] private string _sceneName = "";

#if UNITY_EDITOR
    [SerializeField] private UnityEngine.Object _sceneAsset;
#endif

    public string SceneName => _sceneName;

#if UNITY_EDITOR
    public void SetSceneAsset(SceneAsset asset)
    {
        _sceneAsset = asset;
        _sceneName = asset != null ? asset.name : "";
    }
#endif
}

/// <summary>
/// SceneReference의 커스텀 에디터 드로워 (드래그 앤 드롭 지원)
/// </summary>
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SceneReference))]
public class SceneReferenceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty sceneAssetProp = property.FindPropertyRelative("_sceneAsset");
        SerializedProperty sceneNameProp = property.FindPropertyRelative("_sceneName");

        EditorGUI.BeginChangeCheck();

        // 드래그 앤 드롭으로 SceneAsset 설정
        UnityEngine.Object obj = EditorGUI.ObjectField(position, label.text, sceneAssetProp.objectReferenceValue, typeof(SceneAsset), false);

        if (EditorGUI.EndChangeCheck())
        {
            // sceneAsset이 변경되면 sceneName도 자동 업데이트
            sceneAssetProp.objectReferenceValue = obj;
            SceneAsset asset = obj as SceneAsset;
            if (asset != null)
            {
                sceneNameProp.stringValue = asset.name;
            }
            else
            {
                sceneNameProp.stringValue = "";
            }
        }

        EditorGUI.EndProperty();
    }
}
#endif
