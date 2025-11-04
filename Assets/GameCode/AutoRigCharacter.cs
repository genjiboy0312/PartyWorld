using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class AutoRigCharacter
{
    [MenuItem("Tools/Auto Rig PartyAnimal Style")]
    public static void RigSelectedCharacter()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogError("선택된 캐릭터가 없습니다.");
            return;
        }

        // Animator 설정
        Animator animator = selected.GetComponent<Animator>();
        if (animator == null)
            animator = selected.AddComponent<Animator>();

        animator.applyRootMotion = true;

        // Animator Controller 폴더
        string controllerFolder = "Assets/GameData/AssetFolder/MainCharacterPack/Animations";
        if (!AssetDatabase.IsValidFolder(controllerFolder))
            AssetDatabase.CreateFolder("Assets/GameData/AssetFolder/MainCharacterPack", "Animations");

        string controllerPath = controllerFolder + "/" + selected.name + "_Controller.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        animator.runtimeAnimatorController = controller;

        // Humanoid 관절 가져오기
        var animatorHumanoid = animator.avatar;
        if (!animatorHumanoid.isValid || !animatorHumanoid.isHuman)
        {
            Debug.LogWarning("Humanoid Avatar가 필요합니다.");
        }

        // 목과 Spine에 물리 관절 추가
        Transform neck = FindChildByName(selected.transform, "Neck") ?? FindChildByName(selected.transform, "Head");
        Transform spine = FindChildByName(selected.transform, "Spine");

        if (neck != null) AddJointToBone(neck, selected.transform);
        if (spine != null) AddJointToBone(spine, selected.transform);

        Debug.Log("PartyAnimal 스타일 리깅 완료");
    }

    private static Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform t in parent.GetComponentsInChildren<Transform>())
        {
            if (t.name.ToLower().Contains(name.ToLower()))
                return t;
        }
        return null;
    }

    private static void AddJointToBone(Transform bone, Transform root)
    {
        Rigidbody rb = bone.gameObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = bone.gameObject.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
        }

        ConfigurableJoint joint = bone.gameObject.GetComponent<ConfigurableJoint>();
        if (joint == null)
        {
            joint = bone.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = root.GetComponent<Rigidbody>() ?? root.gameObject.AddComponent<Rigidbody>();
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Limited;

            SoftJointLimit limit = joint.lowAngularXLimit;
            limit.limit = -10;
            joint.lowAngularXLimit = limit;

            limit = joint.highAngularXLimit;
            limit.limit = 10;
            joint.highAngularXLimit = limit;

            SoftJointLimitSpring spring = joint.angularXLimitSpring;
            spring.spring = 20;
            joint.angularXLimitSpring = spring;

            joint.angularYZLimitSpring = spring;
        }
    }
}
