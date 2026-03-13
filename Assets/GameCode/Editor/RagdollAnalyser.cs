using UnityEngine;
using UnityEditor;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class RagdollAnalyser
{
    private static string SanitizeName(string name)
    {
        // Replace invalid characters with underscores
        return Regex.Replace(name, @"[^a-zA-Z0-9_]", "_", RegexOptions.Compiled);
    }

    [MenuItem("Tools/PartyWorld/Character/Analyze Player_Test02")]
    public static void AnalyzeRagdoll()
    {
        GameObject templateObject = GameObject.Find("Player_Test02");
        if (templateObject == null)
        {
            EditorUtility.DisplayDialog("Error", "Scene에 'Player_Test02' 오브젝트가 없습니다. Scene_Main을 열고 다시 시도해주세요.", "OK");
            return;
        }

        Animator animator = templateObject.GetComponentInChildren<Animator>();
        if (animator == null || !animator.isHuman)
        {
            EditorUtility.DisplayDialog("Error", "'Player_Test02' 또는 그 자식 오브젝트에서 Humanoid Animator를 찾을 수 없습니다.", "OK");
            return;
        }

        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine("// --- START OF GENERATED RAGDOLL CODE ---");
        sb.AppendLine("private void ApplyRagdollSettings(Animator animator, GameObject rootObject)");
        sb.AppendLine("{");

        // First, generate code for all components on the root object itself
        GenerateCodeForObject(sb, templateObject, "rootObject");

        // Then, generate code for all children that have a Rigidbody
        var rigidbodies = templateObject.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rigidbodies)
        {
            if (rb.gameObject == templateObject) continue;
            
            string path = AnimationUtility.CalculateTransformPath(rb.transform, templateObject.transform);
            string sanitizedName = SanitizeName(rb.gameObject.name);

            sb.AppendLine($"    // --- Settings for bone: {rb.gameObject.name} ---");
            sb.AppendLine($"    var {sanitizedName}_go = rootObject.transform.Find(\"{path}\")?.gameObject;");
            sb.AppendLine($"    if ({sanitizedName}_go != null)");
            sb.AppendLine( "    {");
            GenerateCodeForObject(sb, rb.gameObject, $"{sanitizedName}_go");
            sb.AppendLine( "    }");
        }
        
        sb.AppendLine("}");
        sb.AppendLine("// --- END OF GENERATED RAGDOLL CODE ---");

        Debug.Log(sb.ToString());
        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        EditorUtility.DisplayDialog("Analysis Complete", "분석이 완료되었습니다. Unity Console 창에서 생성된 코드를 확인하고, 클립보드에도 복사되었습니다. 저에게 붙여넣기 해주세요.", "OK");
    }

    private static void GenerateCodeForObject(StringBuilder sb, GameObject obj, string gameObjectNameInCode)
    {
        string sanitizedName = SanitizeName(obj.name);

        // Rigidbody
        if (obj.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            sb.AppendLine($"        var {sanitizedName}_rb = {gameObjectNameInCode}.AddComponent<Rigidbody>();");
            sb.AppendLine($"        {sanitizedName}_rb.mass = {rb.mass}f;");
            sb.AppendLine($"        {sanitizedName}_rb.linearDamping = {rb.linearDamping}f;");
            sb.AppendLine($"        {sanitizedName}_rb.angularDamping = {rb.angularDamping}f;");
            sb.AppendLine($"        {sanitizedName}_rb.useGravity = {rb.useGravity.ToString().ToLower()};");
            sb.AppendLine($"        {sanitizedName}_rb.isKinematic = {rb.isKinematic.ToString().ToLower()};");
            sb.AppendLine($"        {sanitizedName}_rb.interpolation = RigidbodyInterpolation.{rb.interpolation};");
            sb.AppendLine($"        {sanitizedName}_rb.collisionDetectionMode = CollisionDetectionMode.{rb.collisionDetectionMode};");
            sb.AppendLine($"        {sanitizedName}_rb.constraints = (RigidbodyConstraints){(int)rb.constraints};");
        }

        // CapsuleCollider
        if (obj.TryGetComponent<CapsuleCollider>(out CapsuleCollider capsule))
        {
            sb.AppendLine($"        var {sanitizedName}_cap = {gameObjectNameInCode}.AddComponent<CapsuleCollider>();");
            sb.AppendLine($"        {sanitizedName}_cap.radius = {capsule.radius}f;");
            sb.AppendLine($"        {sanitizedName}_cap.height = {capsule.height}f;");
            sb.AppendLine($"        {sanitizedName}_cap.center = new Vector3({capsule.center.x}f, {capsule.center.y}f, {capsule.center.z}f);");
            sb.AppendLine($"        {sanitizedName}_cap.direction = {capsule.direction};");
        }
        
        // SphereCollider
        if (obj.TryGetComponent<SphereCollider>(out SphereCollider sphere))
        {
            sb.AppendLine($"        var {sanitizedName}_sphere = {gameObjectNameInCode}.AddComponent<SphereCollider>();");
            sb.AppendLine($"        {sanitizedName}_sphere.radius = {sphere.radius}f;");
            sb.AppendLine($"        {sanitizedName}_sphere.center = new Vector3({sphere.center.x}f, {sphere.center.y}f, {sphere.center.z}f);");
        }

        // ConfigurableJoint
        if (obj.TryGetComponent<ConfigurableJoint>(out ConfigurableJoint joint))
        {
            sb.AppendLine($"        var {sanitizedName}_joint = {gameObjectNameInCode}.AddComponent<ConfigurableJoint>();");
            if (joint.connectedBody != null)
            {
                 string connectedBodyPath = AnimationUtility.CalculateTransformPath(joint.connectedBody.transform, joint.transform.root);
                 sb.AppendLine($"        {sanitizedName}_joint.connectedBody = rootObject.transform.Find(\"{connectedBodyPath}\")?.GetComponent<Rigidbody>();");
            }
            sb.AppendLine($"        {sanitizedName}_joint.anchor = new Vector3({joint.anchor.x}f, {joint.anchor.y}f, {joint.anchor.z}f);");
            sb.AppendLine($"        {sanitizedName}_joint.axis = new Vector3({joint.axis.x}f, {joint.axis.y}f, {joint.axis.z}f);");
            sb.AppendLine($"        {sanitizedName}_joint.secondaryAxis = new Vector3({joint.secondaryAxis.x}f, {joint.secondaryAxis.y}f, {joint.secondaryAxis.z}f);");
            sb.AppendLine($"        {sanitizedName}_joint.autoConfigureConnectedAnchor = {joint.autoConfigureConnectedAnchor.ToString().ToLower()};");
            sb.AppendLine($"        {sanitizedName}_joint.connectedAnchor = new Vector3({joint.connectedAnchor.x}f, {joint.connectedAnchor.y}f, {joint.connectedAnchor.z}f);");
            sb.AppendLine($"        {sanitizedName}_joint.xMotion = ConfigurableJointMotion.{joint.xMotion};");
            sb.AppendLine($"        {sanitizedName}_joint.yMotion = ConfigurableJointMotion.{joint.yMotion};");
            sb.AppendLine($"        {sanitizedName}_joint.zMotion = ConfigurableJointMotion.{joint.zMotion};");
            sb.AppendLine($"        {sanitizedName}_joint.angularXMotion = ConfigurableJointMotion.{joint.angularXMotion};");
            sb.AppendLine($"        {sanitizedName}_joint.angularYMotion = ConfigurableJointMotion.{joint.angularYMotion};");
            sb.AppendLine($"        {sanitizedName}_joint.angularZMotion = ConfigurableJointMotion.{joint.angularZMotion};");
            
            sb.AppendLine($"        var {sanitizedName}_limit = new SoftJointLimit();");
            sb.AppendLine($"        {sanitizedName}_limit.limit = {joint.linearLimit.limit}f;");
            sb.AppendLine($"        {sanitizedName}_joint.linearLimit = {sanitizedName}_limit;");
            sb.AppendLine($"        {sanitizedName}_joint.linearLimitSpring = joint.linearLimitSpring;");
            sb.AppendLine($"        {sanitizedName}_joint.lowAngularXLimit = joint.lowAngularXLimit;");
            sb.AppendLine($"        {sanitizedName}_joint.highAngularXLimit = joint.highAngularXLimit;");
            sb.AppendLine($"        {sanitizedName}_joint.angularYLimit = joint.angularYLimit;");
            sb.AppendLine($"        {sanitizedName}_joint.angularZLimit = joint.angularZLimit;");

            sb.AppendLine($"        {sanitizedName}_joint.rotationDriveMode = RotationDriveMode.{joint.rotationDriveMode};");
            sb.AppendLine($"        {sanitizedName}_joint.xDrive = joint.xDrive;");
            sb.AppendLine($"        {sanitizedName}_joint.yDrive = joint.yDrive;");
            sb.AppendLine($"        {sanitizedName}_joint.zDrive = joint.zDrive;");
            sb.AppendLine($"        var {sanitizedName}_xDrive = new JointDrive();");
            sb.AppendLine($"        {sanitizedName}_xDrive.positionSpring = {joint.angularXDrive.positionSpring}f;");
            sb.AppendLine($"        {sanitizedName}_xDrive.maximumForce = {joint.angularXDrive.maximumForce}f;");
            sb.AppendLine($"        {sanitizedName}_joint.angularXDrive = {sanitizedName}_xDrive;");

            sb.AppendLine($"        var {sanitizedName}_yzDrive = new JointDrive();");
            sb.AppendLine($"        {sanitizedName}_yzDrive.positionSpring = {joint.angularYZDrive.positionSpring}f;");
            sb.AppendLine($"        {sanitizedName}_yzDrive.maximumForce = {joint.angularYZDrive.maximumForce}f;");
            sb.AppendLine($"        {sanitizedName}_joint.angularYZDrive = {sanitizedName}_yzDrive;");
            sb.AppendLine($"        {sanitizedName}_joint.slerpDrive = joint.slerpDrive;");
            sb.AppendLine($"        {sanitizedName}_joint.projectionMode = JointProjectionMode.{joint.projectionMode};");
            sb.AppendLine($"        {sanitizedName}_joint.projectionDistance = {joint.projectionDistance}f;");
            sb.AppendLine($"        {sanitizedName}_joint.projectionAngle = {joint.projectionAngle}f;");
            sb.AppendLine($"        {sanitizedName}_joint.breakForce = {joint.breakForce}f;");
            sb.AppendLine($"        {sanitizedName}_joint.breakTorque = {joint.breakTorque}f;");
            sb.AppendLine($"        {sanitizedName}_joint.enableCollision = {joint.enableCollision.ToString().ToLower()};");
            sb.AppendLine($"        {sanitizedName}_joint.enablePreprocessing = {joint.enablePreprocessing.ToString().ToLower()};");
            sb.AppendLine($"        {sanitizedName}_joint.massScale = {joint.massScale}f;");
            sb.AppendLine($"        {sanitizedName}_joint.connectedMassScale = {joint.connectedMassScale}f;");
        }
        sb.AppendLine();
    }
}
