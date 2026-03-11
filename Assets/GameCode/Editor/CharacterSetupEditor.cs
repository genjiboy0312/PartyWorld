using UnityEngine;
using UnityEditor;
using System.IO;
using Photon.Pun; // PhotonView를 위해 추가

public class CharacterSetupEditor : EditorWindow
{
    private GameObject characterModel;

    [MenuItem("PartyWorld Tools/Setup Character From Player_Test02")]
    public static void ShowWindow()
    {
        GetWindow<CharacterSetupEditor>("Setup From Player_Test02");
    }

    private void OnGUI()
    {
        GUILayout.Label("Setup Character from Player_Test02", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("새로운 캐릭터 모델(Humanoid)을 할당하고 버튼을 누르면, 'Player_Test02'와 동일한 구조의 프리팹을 생성합니다.", MessageType.Info);

        characterModel = (GameObject)EditorGUILayout.ObjectField("New Character Model", characterModel, typeof(GameObject), false);

        if (GUILayout.Button("Create Character"))
        {
            if (characterModel == null)
            {
                EditorUtility.DisplayDialog("오류", "새로운 캐릭터 모델을 먼저 할당해주세요.", "확인");
                return;
            }

            Animator animator = characterModel.GetComponent<Animator>();
            if (animator == null || !animator.isHuman)
            {
                EditorUtility.DisplayDialog("오류", "할당된 모델에 Humanoid 설정이 된 Animator 컴포넌트가 없습니다.", "확인");
                return;
            }
            
            CreateRagdollCharacter(animator);
        }
    }

    private void CreateRagdollCharacter(Animator animator)
    {
        GameObject instance = Instantiate(animator.gameObject);
        instance.name = animator.gameObject.name + "_Setup";

        // 기존의 물리 컴포넌트를 모두 제거하여 충돌을 방지합니다.
        foreach(var joint in instance.GetComponentsInChildren<Joint>()) DestroyImmediate(joint);
        foreach(var rb in instance.GetComponentsInChildren<Rigidbody>()) DestroyImmediate(rb);
        foreach(var col in instance.GetComponentsInChildren<Collider>()) DestroyImmediate(col);
        
        // Player_Test02와 동일한 루트 컴포넌트들을 추가합니다.
        instance.AddComponent<PlayerPresenter>();
        instance.AddComponent<IgnoreCollision>();
        instance.AddComponent<PhotonView>();

        // 분석된 코드를 바탕으로 물리 설정을 적용합니다.
        ApplyRagdollSettings(animator, instance);

        // 프리팹으로 저장
        string directoryPath = "Assets/GameData/Prefabs/character";
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string prefabPath = Path.Combine(directoryPath, instance.name + ".prefab");
        prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        DestroyImmediate(instance);

        EditorUtility.DisplayDialog("성공", $"'{savedPrefab.name}' 프리팹이 '{directoryPath}' 폴더에 생성되었습니다.", "확인");
        Selection.activeObject = savedPrefab;
    }

    // 사용자가 제공한 분석 코드를 여기에 붙여넣습니다.
    private void ApplyRagdollSettings(Animator animator, GameObject rootObject)
    {
        var Player_Test02_rb = rootObject.AddComponent<Rigidbody>();
        Player_Test02_rb.mass = 1f;
        Player_Test02_rb.linearDamping = 0f;
        Player_Test02_rb.angularDamping = 0.05f;
        Player_Test02_rb.useGravity = true;
        Player_Test02_rb.isKinematic = false;
        Player_Test02_rb.interpolation = RigidbodyInterpolation.None;
        Player_Test02_rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        Player_Test02_rb.constraints = (RigidbodyConstraints)0;
        var Player_Test02_sphere = rootObject.AddComponent<SphereCollider>();
        Player_Test02_sphere.radius = 0.05f;
        Player_Test02_sphere.center = new Vector3(0f, 0.05f, 0f);
        var Player_Test02_joint = rootObject.AddComponent<ConfigurableJoint>();
        Player_Test02_joint.anchor = new Vector3(0f, 0f, 0f);
        Player_Test02_joint.axis = new Vector3(1f, 0f, 0f);
        Player_Test02_joint.xMotion = ConfigurableJointMotion.Free;
        Player_Test02_joint.yMotion = ConfigurableJointMotion.Free;
        Player_Test02_joint.zMotion = ConfigurableJointMotion.Free;
        Player_Test02_joint.angularXMotion = ConfigurableJointMotion.Locked;
        Player_Test02_joint.angularYMotion = ConfigurableJointMotion.Locked;
        Player_Test02_joint.angularZMotion = ConfigurableJointMotion.Locked;
        var Player_Test02_limit = new SoftJointLimit();
        Player_Test02_limit.limit = 0f;
        Player_Test02_joint.linearLimit = Player_Test02_limit;
        Player_Test02_joint.rotationDriveMode = RotationDriveMode.Slerp;
        var Player_Test02_xDrive = new JointDrive();
        Player_Test02_xDrive.positionSpring = 100f;
        Player_Test02_xDrive.maximumForce = 3.402823E+38f;
        Player_Test02_joint.angularXDrive = Player_Test02_xDrive;
        var Player_Test02_yzDrive = new JointDrive();
        Player_Test02_yzDrive.positionSpring = 0f;
        Player_Test02_yzDrive.maximumForce = 3.402823E+38f;
        Player_Test02_joint.angularYZDrive = Player_Test02_yzDrive;

        // --- Settings for bone: Spine1_M ---
        var Spine1_M_go = rootObject.transform.Find("Character/Root_M/Spine1_M")?.gameObject;
        if (Spine1_M_go != null)
        {
            var Spine1_M_rb = Spine1_M_go.AddComponent<Rigidbody>();
            Spine1_M_rb.mass = 1f;
            Spine1_M_rb.linearDamping = 0f;
            Spine1_M_rb.angularDamping = 0.05f;
            Spine1_M_rb.useGravity = true;
            Spine1_M_rb.isKinematic = false;
            Spine1_M_rb.interpolation = RigidbodyInterpolation.None;
            Spine1_M_rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            Spine1_M_rb.constraints = (RigidbodyConstraints)0;
            var Spine1_M_cap = Spine1_M_go.AddComponent<CapsuleCollider>();
            Spine1_M_cap.radius = 0.05f;
            Spine1_M_cap.height = 0.18f;
            Spine1_M_cap.center = new Vector3(0f, 0f, 0f);
            Spine1_M_cap.direction = 0;
            var Spine1_M_joint = Spine1_M_go.AddComponent<ConfigurableJoint>();
            Spine1_M_joint.connectedBody = rootObject.transform.Find("")?.GetComponent<Rigidbody>();
            Spine1_M_joint.anchor = new Vector3(0f, 0f, 0f);
            Spine1_M_joint.axis = new Vector3(1f, 0f, 0f);
            Spine1_M_joint.xMotion = ConfigurableJointMotion.Locked;
            Spine1_M_joint.yMotion = ConfigurableJointMotion.Locked;
            Spine1_M_joint.zMotion = ConfigurableJointMotion.Locked;
            Spine1_M_joint.angularXMotion = ConfigurableJointMotion.Free;
            Spine1_M_joint.angularYMotion = ConfigurableJointMotion.Free;
            Spine1_M_joint.angularZMotion = ConfigurableJointMotion.Free;
            var Spine1_M_limit = new SoftJointLimit();
            Spine1_M_limit.limit = 0f;
            Spine1_M_joint.linearLimit = Spine1_M_limit;
            Spine1_M_joint.rotationDriveMode = RotationDriveMode.Slerp;
            var Spine1_M_xDrive = new JointDrive();
            Spine1_M_xDrive.positionSpring = 0f;
            Spine1_M_xDrive.maximumForce = 3.402823E+38f;
            Spine1_M_joint.angularXDrive = Spine1_M_xDrive;
            var Spine1_M_yzDrive = new JointDrive();
            Spine1_M_yzDrive.positionSpring = 0f;
            Spine1_M_yzDrive.maximumForce = 3.402823E+38f;
            Spine1_M_joint.angularYZDrive = Spine1_M_yzDrive;
        }
        // --- Settings for bone: Head_M ---
        var Head_M_go = rootObject.transform.Find("Character/Root_M/Spine1_M/Chest_M/Neck_M/Head_M")?.gameObject;
        if (Head_M_go != null)
        {
            var Head_M_rb = Head_M_go.AddComponent<Rigidbody>();
            Head_M_rb.mass = 0.1f;
            Head_M_rb.linearDamping = 0f;
            Head_M_rb.angularDamping = 0.05f;
            Head_M_rb.useGravity = true;
            Head_M_rb.isKinematic = false;
            Head_M_rb.interpolation = RigidbodyInterpolation.None;
            Head_M_rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            Head_M_rb.constraints = (RigidbodyConstraints)0;
            var Head_M_sphere = Head_M_go.AddComponent<SphereCollider>();
            Head_M_sphere.radius = 0.175f;
            Head_M_sphere.center = new Vector3(-0.15f, -0.05f, 0f);
            var Head_M_joint = Head_M_go.AddComponent<ConfigurableJoint>();
            Head_M_joint.connectedBody = rootObject.transform.Find("Character/Root_M/Spine1_M")?.GetComponent<Rigidbody>();
            Head_M_joint.anchor = new Vector3(0f, 0f, 0f);
            Head_M_joint.axis = new Vector3(1f, 0f, 0f);
            Head_M_joint.xMotion = ConfigurableJointMotion.Locked;
            Head_M_joint.yMotion = ConfigurableJointMotion.Locked;
            Head_M_joint.zMotion = ConfigurableJointMotion.Locked;
            Head_M_joint.angularXMotion = ConfigurableJointMotion.Locked;
            Head_M_joint.angularYMotion = ConfigurableJointMotion.Locked;
            Head_M_joint.angularZMotion = ConfigurableJointMotion.Limited;
            var Head_M_limit = new SoftJointLimit();
            Head_M_limit.limit = 0f;
            Head_M_joint.linearLimit = Head_M_limit;
            Head_M_joint.rotationDriveMode = RotationDriveMode.Slerp;
            var Head_M_xDrive = new JointDrive();
            Head_M_xDrive.positionSpring = 50f;
            Head_M_xDrive.maximumForce = 3.402823E+38f;
            Head_M_joint.angularXDrive = Head_M_xDrive;
            var Head_M_yzDrive = new JointDrive();
            Head_M_yzDrive.positionSpring = 0f;
            Head_M_yzDrive.maximumForce = 3.402823E+38f;
            Head_M_joint.angularYZDrive = Head_M_yzDrive;
        }
        // --- Settings for bone: Scapula_L ---
        var Scapula_L_go = rootObject.transform.Find("Character/Root_M/Spine1_M/Chest_M/Scapula_L")?.gameObject;
        if (Scapula_L_go != null)
        {
            var Scapula_L_rb = Scapula_L_go.AddComponent<Rigidbody>();
            Scapula_L_rb.mass = 0.1f;
            Scapula_L_rb.linearDamping = 0f;
            Scapula_L_rb.angularDamping = 0.05f;
            Scapula_L_rb.useGravity = true;
            Scapula_L_rb.isKinematic = false;
            Scapula_L_rb.interpolation = RigidbodyInterpolation.None;
            Scapula_L_rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            Scapula_L_rb.constraints = (RigidbodyConstraints)0;
            var Scapula_L_cap = Scapula_L_go.AddComponent<CapsuleCollider>();
            Scapula_L_cap.radius = 0.03f;
            Scapula_L_cap.height = 0.16f;
            Scapula_L_cap.center = new Vector3(0.075f, 0f, 0f);
            Scapula_L_cap.direction = 0;
            var Scapula_L_joint = Scapula_L_go.AddComponent<ConfigurableJoint>();
            Scapula_L_joint.connectedBody = rootObject.transform.Find("Character/Root_M/Spine1_M")?.GetComponent<Rigidbody>();
            Scapula_L_joint.anchor = new Vector3(0f, 0f, 0f);
            Scapula_L_joint.axis = new Vector3(1f, 0f, 0f);
            Scapula_L_joint.xMotion = ConfigurableJointMotion.Locked;
            Scapula_L_joint.yMotion = ConfigurableJointMotion.Locked;
            Scapula_L_joint.zMotion = ConfigurableJointMotion.Locked;
            Scapula_L_joint.angularXMotion = ConfigurableJointMotion.Free;
            Scapula_L_joint.angularYMotion = ConfigurableJointMotion.Free;
            Scapula_L_joint.angularZMotion = ConfigurableJointMotion.Free;
            var Scapula_L_limit = new SoftJointLimit();
            Scapula_L_limit.limit = 0f;
            Scapula_L_joint.linearLimit = Scapula_L_limit;
            Scapula_L_joint.rotationDriveMode = RotationDriveMode.Slerp;
            var Scapula_L_xDrive = new JointDrive();
            Scapula_L_xDrive.positionSpring = 1f;
            Scapula_L_xDrive.maximumForce = 3.402823E+38f;
            Scapula_L_joint.angularXDrive = Scapula_L_xDrive;
            var Scapula_L_yzDrive = new JointDrive();
            Scapula_L_yzDrive.positionSpring = 0f;
            Scapula_L_yzDrive.maximumForce = 3.402823E+38f;
            Scapula_L_joint.angularYZDrive = Scapula_L_yzDrive;
        }
        // --- Settings for bone: Scapula_R ---
        var Scapula_R_go = rootObject.transform.Find("Character/Root_M/Spine1_M/Chest_M/Scapula_R")?.gameObject;
        if (Scapula_R_go != null)
        {
            var Scapula_R_rb = Scapula_R_go.AddComponent<Rigidbody>();
            Scapula_R_rb.mass = 0.1f;
            Scapula_R_rb.linearDamping = 0f;
            Scapula_R_rb.angularDamping = 0.05f;
            Scapula_R_rb.useGravity = true;
            Scapula_R_rb.isKinematic = false;
            Scapula_R_rb.interpolation = RigidbodyInterpolation.None;
            Scapula_R_rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            Scapula_R_rb.constraints = (RigidbodyConstraints)0;
            var Scapula_R_cap = Scapula_R_go.AddComponent<CapsuleCollider>();
            Scapula_R_cap.radius = 0.03f;
            Scapula_R_cap.height = 0.16f;
            Scapula_R_cap.center = new Vector3(-0.075f, 0f, 0f);
            Scapula_R_cap.direction = 0;
            var Scapula_R_joint = Scapula_R_go.AddComponent<ConfigurableJoint>();
            Scapula_R_joint.connectedBody = rootObject.transform.Find("Character/Root_M/Spine1_M")?.GetComponent<Rigidbody>();
            Scapula_R_joint.anchor = new Vector3(0f, 0f, 0f);
            Scapula_R_joint.axis = new Vector3(1f, 0f, 0f);
            Scapula_R_joint.xMotion = ConfigurableJointMotion.Locked;
            Scapula_R_joint.yMotion = ConfigurableJointMotion.Locked;
            Scapula_R_joint.zMotion = ConfigurableJointMotion.Locked;
            Scapula_R_joint.angularXMotion = ConfigurableJointMotion.Free;
            Scapula_R_joint.angularYMotion = ConfigurableJointMotion.Free;
            Scapula_R_joint.angularZMotion = ConfigurableJointMotion.Free;
            var Scapula_R_limit = new SoftJointLimit();
            Scapula_R_limit.limit = 0f;
            Scapula_R_joint.linearLimit = Scapula_R_limit;
            Scapula_R_joint.rotationDriveMode = RotationDriveMode.Slerp;
            var Scapula_R_xDrive = new JointDrive();
            Scapula_R_xDrive.positionSpring = 1f;
            Scapula_R_xDrive.maximumForce = 3.402823E+38f;
            Scapula_R_joint.angularXDrive = Scapula_R_xDrive;
            var Scapula_R_yzDrive = new JointDrive();
            Scapula_R_yzDrive.positionSpring = 0f;
            Scapula_R_yzDrive.maximumForce = 3.402823E+38f;
            Scapula_R_joint.angularYZDrive = Scapula_R_yzDrive;
        }
        // --- Settings for bone: Hip_L ---
        var Hip_L_go = rootObject.transform.Find("Character/Root_M/Hip_L")?.gameObject;
        if (Hip_L_go != null)
        {
            var Hip_L_rb = Hip_L_go.AddComponent<Rigidbody>();
            Hip_L_rb.mass = 0.1f;
            Hip_L_rb.linearDamping = 0f;
            Hip_L_rb.angularDamping = 0.05f;
            Hip_L_rb.useGravity = true;
            Hip_L_rb.isKinematic = false;
            Hip_L_rb.interpolation = RigidbodyInterpolation.None;
            Hip_L_rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            Hip_L_rb.constraints = (RigidbodyConstraints)0;
            var Hip_L_cap = Hip_L_go.AddComponent<CapsuleCollider>();
            Hip_L_cap.radius = 0.04f;
            Hip_L_cap.height = 0.09f;
            Hip_L_cap.center = new Vector3(0.05f, 0f, 0f);
            Hip_L_cap.direction = 0;
            var Hip_L_joint = Hip_L_go.AddComponent<ConfigurableJoint>();
            Hip_L_joint.connectedBody = rootObject.transform.Find("Character/Root_M/Spine1_M")?.GetComponent<Rigidbody>();
            Hip_L_joint.anchor = new Vector3(0f, 0f, 0f);
            Hip_L_joint.axis = new Vector3(1f, 0f, 0f);
            Hip_L_joint.xMotion = ConfigurableJointMotion.Locked;
            Hip_L_joint.yMotion = ConfigurableJointMotion.Locked;
            Hip_L_joint.zMotion = ConfigurableJointMotion.Locked;
            Hip_L_joint.angularXMotion = ConfigurableJointMotion.Free;
            Hip_L_joint.angularYMotion = ConfigurableJointMotion.Free;
            Hip_L_joint.angularZMotion = ConfigurableJointMotion.Free;
            var Hip_L_limit = new SoftJointLimit();
            Hip_L_limit.limit = 0f;
            Hip_L_joint.linearLimit = Hip_L_limit;
            Hip_L_joint.rotationDriveMode = RotationDriveMode.Slerp;
            var Hip_L_xDrive = new JointDrive();
            Hip_L_xDrive.positionSpring = 0f;
            Hip_L_xDrive.maximumForce = 3.402823E+38f;
            Hip_L_joint.angularXDrive = Hip_L_xDrive;
            var Hip_L_yzDrive = new JointDrive();
            Hip_L_yzDrive.positionSpring = 0f;
            Hip_L_yzDrive.maximumForce = 3.402823E+38f;
            Hip_L_joint.angularYZDrive = Hip_L_yzDrive;
        }
        // --- Settings for bone: Hip_R ---
        var Hip_R_go = rootObject.transform.Find("Character/Root_M/Hip_R")?.gameObject;
        if (Hip_R_go != null)
        {
            var Hip_R_rb = Hip_R_go.AddComponent<Rigidbody>();
            Hip_R_rb.mass = 0.1f;
            Hip_R_rb.linearDamping = 0f;
            Hip_R_rb.angularDamping = 0.05f;
            Hip_R_rb.useGravity = true;
            Hip_R_rb.isKinematic = false;
            Hip_R_rb.interpolation = RigidbodyInterpolation.None;
            Hip_R_rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            Hip_R_rb.constraints = (RigidbodyConstraints)0;
            var Hip_R_cap = Hip_R_go.AddComponent<CapsuleCollider>();
            Hip_R_cap.radius = 0.04f;
            Hip_R_cap.height = 0.09f;
            Hip_R_cap.center = new Vector3(-0.05f, 0f, 0f);
            Hip_R_cap.direction = 0;
            var Hip_R_joint = Hip_R_go.AddComponent<ConfigurableJoint>();
            Hip_R_joint.connectedBody = rootObject.transform.Find("Character/Root_M/Spine1_M")?.GetComponent<Rigidbody>();
            Hip_R_joint.anchor = new Vector3(0f, 0f, 0f);
            Hip_R_joint.axis = new Vector3(1f, 0f, 0f);
            Hip_R_joint.xMotion = ConfigurableJointMotion.Locked;
            Hip_R_joint.yMotion = ConfigurableJointMotion.Locked;
            Hip_R_joint.zMotion = ConfigurableJointMotion.Locked;
            Hip_R_joint.angularXMotion = ConfigurableJointMotion.Free;
            Hip_R_joint.angularYMotion = ConfigurableJointMotion.Free;
            Hip_R_joint.angularZMotion = ConfigurableJointMotion.Free;
            var Hip_R_limit = new SoftJointLimit();
            Hip_R_limit.limit = 0f;
            Hip_R_joint.linearLimit = Hip_R_limit;
            Hip_R_joint.rotationDriveMode = RotationDriveMode.Slerp;
            var Hip_R_xDrive = new JointDrive();
            Hip_R_xDrive.positionSpring = 0f;
            Hip_R_xDrive.maximumForce = 3.402823E+38f;
            Hip_R_joint.angularXDrive = Hip_R_xDrive;
            var Hip_R_yzDrive = new JointDrive();
            Hip_R_yzDrive.positionSpring = 0f;
            Hip_R_yzDrive.maximumForce = 3.402823E+38f;
            Hip_R_joint.angularYZDrive = Hip_R_yzDrive;
        }
    }
}
