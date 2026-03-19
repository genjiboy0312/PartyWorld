using System;
using System.Collections.Generic;
using System.IO;
using Photon.Pun;
using UnityEditor;
using UnityEngine;

public class CharacterSetupEditor : EditorWindow
{
    private const string TEMPLATE_NAME = "Player_CreateTemplate";

    [SerializeField] private GameObject _templateRoot;
    [SerializeField] private GameObject _characterModel;
    [SerializeField] private bool _copyGameCodeScripts = true;
    [SerializeField] private bool _copyPhysicsSettings = true;
    [SerializeField] private bool _copyPresenterSettings = true;
    [SerializeField] private bool _copyIgnoreCollisionSettings = true;
    [SerializeField] private bool _copySoftFollowHeadSettings = true;
    [SerializeField] private bool _setupPhotonViewObserved = true;
    [SerializeField] private bool _disableAnimatorComponent = false;

    [MenuItem("Tools/PartyWorld/Character/Setup From Player_CreateTemplate")]
    public static void ShowWindow()
    {
        GetWindow<CharacterSetupEditor>("Setup From Player_CreateTemplate");
    }

    private void OnGUI()
    {
        GUILayout.Label("Setup Character from Player_CreateTemplate", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("새로운 캐릭터 모델(Humanoid)을 선택하면, 'Player_CreateTemplate'의 설정을 기준으로 프리팹을 생성합니다. (프로토타입용)", MessageType.Info);

        _templateRoot = (GameObject)EditorGUILayout.ObjectField("Template Root", _templateRoot, typeof(GameObject), true);
        if (_templateRoot == null)
        {
            _templateRoot = FindSceneObjectIncludingInactive(TEMPLATE_NAME);
        }

        _characterModel = (GameObject)EditorGUILayout.ObjectField("New Character Model", _characterModel, typeof(GameObject), false);

        GUILayout.Space(8);
        GUILayout.Label("Copy Options", EditorStyles.boldLabel);
        _copyGameCodeScripts = EditorGUILayout.ToggleLeft("Copy GameCode scripts (allowlist)", _copyGameCodeScripts);
        _copyPhysicsSettings = EditorGUILayout.ToggleLeft("Copy physics (RB/Collider/ConfigurableJoint)", _copyPhysicsSettings);
        _copyPresenterSettings = EditorGUILayout.ToggleLeft("Copy PlayerPresenter settings", _copyPresenterSettings);
        _copyIgnoreCollisionSettings = EditorGUILayout.ToggleLeft("Copy IgnoreCollision settings", _copyIgnoreCollisionSettings);
        _copySoftFollowHeadSettings = EditorGUILayout.ToggleLeft("Copy SoftFollowHead settings", _copySoftFollowHeadSettings);
        _setupPhotonViewObserved = EditorGUILayout.ToggleLeft("Setup PhotonView ObservedComponents", _setupPhotonViewObserved);
        _disableAnimatorComponent = EditorGUILayout.ToggleLeft("Disable Animator component on output", _disableAnimatorComponent);

        if (GUILayout.Button("Create Character Prefab"))
        {
            if (!ValidateInputs(out Animator templateAnimator, out Animator newAnimator))
                return;

            CreateCharacterPrefab(_templateRoot, templateAnimator, newAnimator);
        }
    }

    private bool ValidateInputs(out Animator templateAnimator, out Animator newAnimator)
    {
        templateAnimator = null;
        newAnimator = null;

        if (_templateRoot == null)
        {
            EditorUtility.DisplayDialog("오류", $"씬에서 '{TEMPLATE_NAME}' 오브젝트를 찾지 못했습니다. Template Root를 수동으로 할당해주세요.", "확인");
            return false;
        }

        if (_characterModel == null)
        {
            EditorUtility.DisplayDialog("오류", "새로운 캐릭터 모델을 먼저 할당해주세요.", "확인");
            return false;
        }

        templateAnimator = _templateRoot.GetComponentInChildren<Animator>();
        if (templateAnimator == null || !templateAnimator.isHuman)
        {
            EditorUtility.DisplayDialog("오류", $"'{TEMPLATE_NAME}'에서 Humanoid Animator를 찾을 수 없습니다.", "확인");
            return false;
        }

        newAnimator = _characterModel.GetComponent<Animator>();
        if (newAnimator == null || !newAnimator.isHuman)
        {
            EditorUtility.DisplayDialog("오류", "할당된 모델에 Humanoid 설정이 된 Animator 컴포넌트가 없습니다.", "확인");
            return false;
        }

        return true;
    }

    private void CreateCharacterPrefab(GameObject templateRoot, Animator templateAnimator, Animator newAnimator)
    {
        // 새 모델을 인스턴스화해서 프리팹으로 저장
        GameObject instance = Instantiate(newAnimator.gameObject);
        instance.name = newAnimator.gameObject.name + "_Setup";

        // 기존 컴포넌트를 정리(충돌/중복 방지)
        if (_copyPhysicsSettings)
        {
            foreach (var joint in instance.GetComponentsInChildren<Joint>(true)) DestroyImmediate(joint);
            foreach (var rb in instance.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(rb);
            foreach (var col in instance.GetComponentsInChildren<Collider>(true)) DestroyImmediate(col);
        }

        // 루트 스크립트는 기본 추가(프로젝트 런타임과 호환)
        PlayerPresenter presenter = instance.GetComponent<PlayerPresenter>();
        if (presenter == null) presenter = instance.AddComponent<PlayerPresenter>();

        IgnoreCollision ignoreCollision = instance.GetComponent<IgnoreCollision>();
        if (ignoreCollision == null) ignoreCollision = instance.AddComponent<IgnoreCollision>();

        PhotonView photonView = instance.GetComponent<PhotonView>();
        if (photonView == null) photonView = instance.AddComponent<PhotonView>();

        Animator instanceAnimator = instance.GetComponentInChildren<Animator>();
        if (_disableAnimatorComponent && instanceAnimator != null)
            instanceAnimator.enabled = false;
        Dictionary<Transform, Transform> transformMap = BuildTransformMap(templateRoot.transform, templateAnimator, instance.transform, instanceAnimator);

        if (_copyPhysicsSettings)
            CopyPhysicsFromTemplate(templateRoot, templateAnimator, instance, instanceAnimator, transformMap);

        if (_copyGameCodeScripts)
            CopyGameCodeScriptsAllowList(templateRoot, instance, transformMap);

        FixupRootComponents(templateRoot, instance, transformMap);

        if (_copyPresenterSettings)
            CopyAndFixupPresenter(templateRoot, instance);

        CopyAndFixupPlayerView(templateRoot, instance);

        if (_copyIgnoreCollisionSettings)
            CopyAndFixupIgnoreCollision(templateRoot, instance, transformMap);

        if (_copySoftFollowHeadSettings)
            CopyAndFixupSoftFollowHead(templateRoot, instance, transformMap);

        if (_setupPhotonViewObserved)
            SetupPhotonViewObserved(photonView, presenter);

        string directoryPath = "Assets/GameData/Prefabs/character";
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        string prefabPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directoryPath, instance.name + ".prefab"));
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        DestroyImmediate(instance);

        EditorUtility.DisplayDialog("성공", $"'{savedPrefab.name}' 프리팹이 '{directoryPath}' 폴더에 생성되었습니다.", "확인");
        Selection.activeObject = savedPrefab;
    }

    private static void CopyPhysicsFromTemplate(GameObject templateRoot, Animator templateAnimator, GameObject newRoot, Animator newAnimator, Dictionary<Transform, Transform> transformMap)
    {
        // 1) Rigidbody 복사
        foreach (Rigidbody srcRb in templateRoot.GetComponentsInChildren<Rigidbody>(true))
        {
            Transform targetTransform = ResolveTargetTransform(srcRb.transform, templateRoot.transform, newRoot.transform, transformMap);
            if (targetTransform == null)
                continue;

            CopyRigidbody(srcRb, targetTransform.gameObject);
            targetTransform.gameObject.layer = srcRb.gameObject.layer;
        }

        // 2) Collider 복사
        foreach (Collider srcCol in templateRoot.GetComponentsInChildren<Collider>(true))
        {
            Transform targetTransform = ResolveTargetTransform(srcCol.transform, templateRoot.transform, newRoot.transform, transformMap);
            if (targetTransform == null)
                continue;

            CopyCollider(srcCol, targetTransform.gameObject);
            targetTransform.gameObject.layer = srcCol.gameObject.layer;
        }

        // 3) ConfigurableJoint 복사(connectedBody 포함)
        foreach (ConfigurableJoint srcJoint in templateRoot.GetComponentsInChildren<ConfigurableJoint>(true))
        {
            Transform targetTransform = ResolveTargetTransform(srcJoint.transform, templateRoot.transform, newRoot.transform, transformMap);
            if (targetTransform == null)
                continue;

            Rigidbody connectedTargetBody = null;
            if (srcJoint.connectedBody != null)
            {
                Transform connectedTargetTransform = ResolveTargetTransform(srcJoint.connectedBody.transform, templateRoot.transform, newRoot.transform, transformMap);
                if (connectedTargetTransform != null)
                    connectedTargetBody = connectedTargetTransform.GetComponent<Rigidbody>();
            }

            CopyConfigurableJoint(srcJoint, targetTransform.gameObject, connectedTargetBody);
            targetTransform.gameObject.layer = srcJoint.gameObject.layer;
        }
    }

    private static void CopyGameCodeScriptsAllowList(GameObject templateRoot, GameObject newRoot, Dictionary<Transform, Transform> transformMap)
    {
        // 템플릿에 붙은 GameCode 스크립트 중 일부만 복사(씬 참조가 많은 스크립트는 별도 Fixup)
        HashSet<Type> allowList = new HashSet<Type>
        {
            typeof(SoftFollowHead)
        };

        foreach (Transform templateTr in templateRoot.GetComponentsInChildren<Transform>(true))
        {
            Transform targetTr = ResolveTargetTransform(templateTr, templateRoot.transform, newRoot.transform, transformMap);
            if (targetTr == null)
                continue;

            foreach (MonoBehaviour src in templateTr.GetComponents<MonoBehaviour>())
            {
                if (src == null)
                    continue;

                Type t = src.GetType();
                if (!allowList.Contains(t))
                    continue;

                MonoBehaviour dst = targetTr.GetComponent(t) as MonoBehaviour;
                if (dst == null)
                    dst = targetTr.gameObject.AddComponent(t) as MonoBehaviour;

                if (dst == null)
                    continue;

                EditorUtility.CopySerialized(src, dst);
            }
        }
    }

    private static void FixupRootComponents(GameObject templateRoot, GameObject newRoot, Dictionary<Transform, Transform> transformMap)
    {
        // 루트 Rigidbody/Joint가 필요한 스크립트들이 있으므로 존재만 보장
        Rigidbody rootRb = newRoot.GetComponent<Rigidbody>();
        if (rootRb == null)
            rootRb = newRoot.AddComponent<Rigidbody>();

        ConfigurableJoint mainJoint = newRoot.GetComponent<ConfigurableJoint>();
        if (mainJoint == null)
            mainJoint = newRoot.AddComponent<ConfigurableJoint>();

        // IgnoreCollision이 사용하는 루트 collider 보장
        SphereCollider rootSphere = newRoot.GetComponent<SphereCollider>();
        if (rootSphere == null)
            rootSphere = newRoot.AddComponent<SphereCollider>();
    }

    private static void CopyAndFixupPresenter(GameObject templateRoot, GameObject newRoot)
    {
        PlayerPresenter src = templateRoot.GetComponent<PlayerPresenter>();
        PlayerPresenter dst = newRoot.GetComponent<PlayerPresenter>();
        if (src == null || dst == null)
            return;

        // 숫자/설정 값 복사(씬 참조는 복사하지 않음)
        SerializedObject soSrc = new SerializedObject(src);
        SerializedObject soDst = new SerializedObject(dst);

        CopyIfExists(soSrc, soDst, "_moveSpeed");
        CopyIfExists(soSrc, soDst, "_jumpForce");
        CopyIfExists(soSrc, soDst, "_groundCheckDistance");
        CopyIfExists(soSrc, soDst, "_groundCheckRadius");
        CopyIfExists(soSrc, soDst, "_groundStickForce");

        SerializedProperty modelSrc = soSrc.FindProperty("_model");
        SerializedProperty modelDst = soDst.FindProperty("_model");
        if (modelSrc != null && modelDst != null)
            CopySerializedPropertyValue(modelSrc, modelDst);

        // 필수 레퍼런스는 새 프리팹 기준으로 재연결
        SetObjectReferenceIfExists(soDst, "_pv", newRoot.GetComponent<PhotonView>());
        SetObjectReferenceIfExists(soDst, "_view", newRoot.GetComponent<PlayerView>());
        SetObjectReferenceIfExists(soDst, "_rigidbody3D", newRoot.GetComponent<Rigidbody>());
        SetObjectReferenceIfExists(soDst, "_mainJoint", newRoot.GetComponent<ConfigurableJoint>());

        // 씬 전용 UI 참조는 비움
        SetObjectReferenceIfExists(soDst, "_controller", null);
        SetObjectReferenceIfExists(soDst, "_btnJump", null);
        SetObjectReferenceIfExists(soDst, "_btnDive", null);
        SetObjectReferenceIfExists(soDst, "_btnGrap", null);

        soDst.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CopyAndFixupPlayerView(GameObject templateRoot, GameObject newRoot)
    {
        PlayerView src = templateRoot.GetComponent<PlayerView>();
        PlayerView dst = newRoot.GetComponent<PlayerView>();
        if (src == null || dst == null)
            return;

        // PlayerView는 물리/애니메이션 파라미터 관련 값을 복사
        EditorUtility.CopySerialized(src, dst);
    }

    private static void CopyAndFixupIgnoreCollision(GameObject templateRoot, GameObject newRoot, Dictionary<Transform, Transform> transformMap)
    {
        IgnoreCollision src = templateRoot.GetComponent<IgnoreCollision>();
        IgnoreCollision dst = newRoot.GetComponent<IgnoreCollision>();
        if (src == null || dst == null)
            return;

        // 기본 값 복사 후, 콜라이더 레퍼런스는 새 프리팹 기준으로 재연결
        EditorUtility.CopySerialized(src, dst);
        SerializedObject so = new SerializedObject(dst);

        SerializedProperty colliderProp = so.FindProperty("_collider");
        if (colliderProp != null)
            colliderProp.objectReferenceValue = newRoot.GetComponent<Collider>();

        SerializedProperty ignoreListProp = so.FindProperty("_ignoreCollider");
        if (ignoreListProp != null && ignoreListProp.isArray)
        {
            List<Collider> mapped = new List<Collider>();

            SerializedObject soSrc = new SerializedObject(src);
            SerializedProperty srcList = soSrc.FindProperty("_ignoreCollider");

            if (srcList != null && srcList.isArray)
            {
                for (int i = 0; i < srcList.arraySize; i++)
                {
                    Collider srcCol = srcList.GetArrayElementAtIndex(i).objectReferenceValue as Collider;
                    if (srcCol == null)
                        continue;

                    Transform targetTr = ResolveTargetTransform(srcCol.transform, templateRoot.transform, newRoot.transform, transformMap);
                    if (targetTr == null)
                        continue;

                    Collider dstCol = targetTr.GetComponent<Collider>();
                    if (dstCol != null)
                        mapped.Add(dstCol);
                }
            }

            ignoreListProp.arraySize = mapped.Count;
            for (int i = 0; i < mapped.Count; i++)
                ignoreListProp.GetArrayElementAtIndex(i).objectReferenceValue = mapped[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CopyAndFixupSoftFollowHead(GameObject templateRoot, GameObject newRoot, Dictionary<Transform, Transform> transformMap)
    {
        SoftFollowHead[] srcHeads = templateRoot.GetComponentsInChildren<SoftFollowHead>(true);
        if (srcHeads == null || srcHeads.Length == 0)
            return;

        Rigidbody rootRb = newRoot.GetComponent<Rigidbody>();

        foreach (SoftFollowHead src in srcHeads)
        {
            Transform targetTr = ResolveTargetTransform(src.transform, templateRoot.transform, newRoot.transform, transformMap);
            if (targetTr == null)
                continue;

            SoftFollowHead dst = targetTr.GetComponent<SoftFollowHead>();
            if (dst == null)
                dst = targetTr.gameObject.AddComponent<SoftFollowHead>();

            EditorUtility.CopySerialized(src, dst);

            SerializedObject so = new SerializedObject(dst);

            // 루트 RB는 항상 새 캐릭터 루트로
            SerializedProperty rootProp = so.FindProperty("_rootRb");
            if (rootProp != null)
                rootProp.objectReferenceValue = rootRb;

            // 타겟은 템플릿의 타겟을 맵핑해서 연결
            SerializedObject soSrc = new SerializedObject(src);
            SerializedProperty srcTargetProp = soSrc.FindProperty("_target");
            if (srcTargetProp != null)
            {
                Transform srcTarget = srcTargetProp.objectReferenceValue as Transform;
                if (srcTarget != null)
                {
                    Transform mappedTarget = ResolveTargetTransform(srcTarget, templateRoot.transform, newRoot.transform, transformMap);
                    SerializedProperty dstTargetProp = so.FindProperty("_target");
                    if (dstTargetProp != null)
                        dstTargetProp.objectReferenceValue = mappedTarget;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            // SoftFollowHead는 Rigidbody가 필요
            if (targetTr.GetComponent<Rigidbody>() == null)
                targetTr.gameObject.AddComponent<Rigidbody>();
        }
    }

    private static void SetupPhotonViewObserved(PhotonView photonView, PlayerPresenter presenter)
    {
        if (photonView == null || presenter == null)
            return;

        // 관측 대상에 PlayerPresenter를 넣어 동기화가 끊기지 않게 보장
        if (photonView.ObservedComponents == null)
            photonView.ObservedComponents = new List<Component>();

        if (!photonView.ObservedComponents.Contains(presenter))
            photonView.ObservedComponents.Add(presenter);
    }

    private static void CopyIfExists(SerializedObject soSrc, SerializedObject soDst, string propName)
    {
        SerializedProperty src = soSrc.FindProperty(propName);
        SerializedProperty dst = soDst.FindProperty(propName);
        if (src == null || dst == null)
            return;

        CopySerializedPropertyValue(src, dst);
    }

    private static bool CopySerializedPropertyValue(SerializedProperty src, SerializedProperty dst)
    {
        if (src == null || dst == null)
            return false;

        if (src.propertyType != dst.propertyType)
            return false;

        switch (src.propertyType)
        {
            case SerializedPropertyType.Integer:
                dst.intValue = src.intValue;
                return true;
            case SerializedPropertyType.Boolean:
                dst.boolValue = src.boolValue;
                return true;
            case SerializedPropertyType.Float:
                dst.floatValue = src.floatValue;
                return true;
            case SerializedPropertyType.String:
                dst.stringValue = src.stringValue;
                return true;
            case SerializedPropertyType.Color:
                dst.colorValue = src.colorValue;
                return true;
            case SerializedPropertyType.ObjectReference:
                dst.objectReferenceValue = src.objectReferenceValue;
                return true;
            case SerializedPropertyType.LayerMask:
                dst.intValue = src.intValue;
                return true;
            case SerializedPropertyType.Enum:
                dst.enumValueIndex = src.enumValueIndex;
                return true;
            case SerializedPropertyType.Vector2:
                dst.vector2Value = src.vector2Value;
                return true;
            case SerializedPropertyType.Vector3:
                dst.vector3Value = src.vector3Value;
                return true;
            case SerializedPropertyType.Vector4:
                dst.vector4Value = src.vector4Value;
                return true;
            case SerializedPropertyType.Rect:
                dst.rectValue = src.rectValue;
                return true;
            case SerializedPropertyType.Bounds:
                dst.boundsValue = src.boundsValue;
                return true;
            case SerializedPropertyType.Quaternion:
                dst.quaternionValue = src.quaternionValue;
                return true;
            case SerializedPropertyType.Vector2Int:
                dst.vector2IntValue = src.vector2IntValue;
                return true;
            case SerializedPropertyType.Vector3Int:
                dst.vector3IntValue = src.vector3IntValue;
                return true;
            case SerializedPropertyType.RectInt:
                dst.rectIntValue = src.rectIntValue;
                return true;
            case SerializedPropertyType.BoundsInt:
                dst.boundsIntValue = src.boundsIntValue;
                return true;
            case SerializedPropertyType.AnimationCurve:
                dst.animationCurveValue = src.animationCurveValue;
                return true;
            case SerializedPropertyType.ExposedReference:
                dst.exposedReferenceValue = src.exposedReferenceValue;
                return true;
            case SerializedPropertyType.ManagedReference:
                dst.managedReferenceValue = src.managedReferenceValue;
                return true;
            case SerializedPropertyType.Generic:
            default:
                return false;
        }
    }

    private static void SetObjectReferenceIfExists(SerializedObject so, string propName, UnityEngine.Object value)
    {
        if (so == null)
            return;

        SerializedProperty prop = so.FindProperty(propName);
        if (prop == null)
            return;

        prop.objectReferenceValue = value;
    }

    private static GameObject FindSceneObjectIncludingInactive(string name)
    {
        // 비활성 오브젝트도 포함해서 현재 로드된 씬에서 탐색
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in all)
        {
            if (go == null)
                continue;

            if (EditorUtility.IsPersistent(go))
                continue;

            if (!go.scene.IsValid() || !go.scene.isLoaded)
                continue;

            if (go.name == name)
                return go;
        }

        return null;
    }

    private static Dictionary<Transform, Transform> BuildTransformMap(Transform templateRoot, Animator templateAnimator, Transform newRoot, Animator newAnimator)
    {
        Dictionary<Transform, Transform> map = new Dictionary<Transform, Transform>();

        if (templateAnimator != null && newAnimator != null && templateAnimator.isHuman && newAnimator.isHuman)
        {
            foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (bone == HumanBodyBones.LastBone)
                    continue;

                Transform templateBone = templateAnimator.GetBoneTransform(bone);
                Transform newBone = newAnimator.GetBoneTransform(bone);
                if (templateBone == null || newBone == null)
                    continue;

                if (!map.ContainsKey(templateBone))
                    map.Add(templateBone, newBone);
            }
        }

        if (!map.ContainsKey(templateRoot))
            map.Add(templateRoot, newRoot);

        return map;
    }

    private static Transform ResolveTargetTransform(Transform templateTransform, Transform templateRoot, Transform newRoot, Dictionary<Transform, Transform> map)
    {
        if (templateTransform == null)
            return null;

        if (map != null && map.TryGetValue(templateTransform, out Transform mapped))
            return mapped;

        string path = AnimationUtility.CalculateTransformPath(templateTransform, templateRoot);
        if (string.IsNullOrWhiteSpace(path))
            return newRoot;

        return newRoot.Find(path);
    }

    private static void CopyRigidbody(Rigidbody src, GameObject target)
    {
        if (src == null || target == null)
            return;

        Rigidbody dst = target.GetComponent<Rigidbody>();
        if (dst == null)
            dst = target.AddComponent<Rigidbody>();

        dst.mass = src.mass;
        dst.linearDamping = src.linearDamping;
        dst.angularDamping = src.angularDamping;
        dst.useGravity = src.useGravity;
        dst.isKinematic = src.isKinematic;
        dst.interpolation = src.interpolation;
        dst.collisionDetectionMode = src.collisionDetectionMode;
        dst.constraints = src.constraints;
    }

    private static void CopyCollider(Collider src, GameObject target)
    {
        if (src == null || target == null)
            return;

        // 지원 범위 외 콜라이더는 필요 시 추가 구현
        if (src is CapsuleCollider srcCapsule)
        {
            CapsuleCollider dst = target.AddComponent<CapsuleCollider>();
            dst.enabled = srcCapsule.enabled;
            dst.radius = srcCapsule.radius;
            dst.height = srcCapsule.height;
            dst.center = srcCapsule.center;
            dst.direction = srcCapsule.direction;
            dst.isTrigger = srcCapsule.isTrigger;
            dst.material = srcCapsule.material;
            dst.contactOffset = srcCapsule.contactOffset;
            return;
        }

        if (src is SphereCollider srcSphere)
        {
            SphereCollider dst = target.AddComponent<SphereCollider>();
            dst.enabled = srcSphere.enabled;
            dst.radius = srcSphere.radius;
            dst.center = srcSphere.center;
            dst.isTrigger = srcSphere.isTrigger;
            dst.material = srcSphere.material;
            dst.contactOffset = srcSphere.contactOffset;
            return;
        }

        if (src is BoxCollider srcBox)
        {
            BoxCollider dst = target.AddComponent<BoxCollider>();
            dst.enabled = srcBox.enabled;
            dst.size = srcBox.size;
            dst.center = srcBox.center;
            dst.isTrigger = srcBox.isTrigger;
            dst.material = srcBox.material;
            dst.contactOffset = srcBox.contactOffset;
            return;
        }
    }

    private static void CopyConfigurableJoint(ConfigurableJoint src, GameObject target, Rigidbody connectedBody)
    {
        if (src == null || target == null)
            return;

        ConfigurableJoint dst = target.AddComponent<ConfigurableJoint>();
        dst.connectedBody = connectedBody;

        dst.anchor = src.anchor;
        dst.axis = src.axis;
        dst.secondaryAxis = src.secondaryAxis;
        dst.autoConfigureConnectedAnchor = src.autoConfigureConnectedAnchor;
        dst.connectedAnchor = src.connectedAnchor;

        dst.xMotion = src.xMotion;
        dst.yMotion = src.yMotion;
        dst.zMotion = src.zMotion;
        dst.angularXMotion = src.angularXMotion;
        dst.angularYMotion = src.angularYMotion;
        dst.angularZMotion = src.angularZMotion;

        dst.linearLimit = src.linearLimit;
        dst.linearLimitSpring = src.linearLimitSpring;

        dst.lowAngularXLimit = src.lowAngularXLimit;
        dst.highAngularXLimit = src.highAngularXLimit;
        dst.angularYLimit = src.angularYLimit;
        dst.angularZLimit = src.angularZLimit;

        dst.targetPosition = src.targetPosition;
        dst.targetVelocity = src.targetVelocity;
        dst.targetRotation = src.targetRotation;
        dst.targetAngularVelocity = src.targetAngularVelocity;

        dst.rotationDriveMode = src.rotationDriveMode;
        dst.xDrive = src.xDrive;
        dst.yDrive = src.yDrive;
        dst.zDrive = src.zDrive;
        dst.angularXDrive = src.angularXDrive;
        dst.angularYZDrive = src.angularYZDrive;
        dst.slerpDrive = src.slerpDrive;

        dst.projectionMode = src.projectionMode;
        dst.projectionDistance = src.projectionDistance;
        dst.projectionAngle = src.projectionAngle;

        dst.breakForce = src.breakForce;
        dst.breakTorque = src.breakTorque;
        dst.enableCollision = src.enableCollision;
        dst.enablePreprocessing = src.enablePreprocessing;
        dst.massScale = src.massScale;
        dst.connectedMassScale = src.connectedMassScale;

        dst.configuredInWorldSpace = src.configuredInWorldSpace;
        dst.swapBodies = src.swapBodies;
    }
}
