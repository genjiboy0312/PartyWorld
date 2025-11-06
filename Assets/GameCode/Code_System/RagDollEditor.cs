using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

public class RagDollEditor : EditorWindow
{
    //PUBLIC VARIABLES
    public GameObject m_Ragdoll_Object;
    //PRIVATE VARIABLES
    //RigidBodies on the Ragdoll
    [SerializeField]
    private Rigidbody[] m_Rigid_Bodies;
    //Percentage of mass in each RB
    private Dictionary<Rigidbody, float> m_Rigid_Bodies_Percent = new Dictionary<Rigidbody, float>();
    //Total Mass of the Ragdoll's rigibodies
    private float m_Total_RB_Mass = 0f;

    private bool m_isCalculated = false;
    //Avoid GO swap
    private GameObject m_Current_RagdollObject;
    //New Mass
    private float m_New_Ragdoll_Mass = 0f;

    //UNITY METHODS
    public void OnInspectorUpdate()
    {
        if (m_Ragdoll_Object == null || m_Current_RagdollObject != m_Ragdoll_Object || m_Ragdoll_Object == m_Current_RagdollObject)

            ResetInformations();
        // This will only get called 10 times per second.
        Repaint();
    }
    private void OnGUI()
    {
        //Label for object
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Ragdoll Object", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        //Object field
        EditorGUILayout.BeginHorizontal();
        m_Ragdoll_Object = (GameObject)EditorGUILayout.ObjectField(m_Ragdoll_Object, typeof(GameObject), true);
        EditorGUILayout.EndHorizontal();


        if (m_Ragdoll_Object != null && !m_isCalculated)
        {
            m_Current_RagdollObject = m_Ragdoll_Object;
            GetRagdollInfo(m_Ragdoll_Object);
            m_isCalculated = true;
        }

        if (m_Rigid_Bodies != null)
        {
            //Current Ragdoll informations
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Ragdoll Informations", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Current Ragdoll Mass ", Math.Round(m_Total_RB_Mass).ToString());

            //Displaying RigidBodies for easiest finding
            ScriptableObject target = this;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty RBs = so.FindProperty("m_Rigid_Bodies");
            EditorGUILayout.PropertyField(RBs, true); // True means show children
            so.ApplyModifiedProperties(); // Remember to apply modified properties

            //New mass Display
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("New Mass", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            GUILayout.Label("Enter new total mass");
            m_New_Ragdoll_Mass = EditorGUILayout.FloatField(m_New_Ragdoll_Mass);

            //On click we launch the mass calculation and apply it to the RBs
            if (GUILayout.Button("Set new mass"))
            {
                if (m_New_Ragdoll_Mass > 0f)
                {
                    SetNewMass(m_New_Ragdoll_Mass);
                    ShowNotification(new GUIContent("Total mass updated"));
                }
                else
                    ShowNotification(new GUIContent("Mass is equal to 0"));
            }
        }
    }
    //METHODS
    [MenuItem("Window/Custom/Ragdoll Mass Editor")]
    public static void ShowWindow()
    {
        //Open window
        GetWindow<RagDollEditor>("Ragdoll Mass Editor");
    }
    //Reset components and info
    private void ResetInformations()
    {
        m_Rigid_Bodies = null;
        m_Total_RB_Mass = 0f;
        m_isCalculated = false;
        m_Current_RagdollObject = m_Ragdoll_Object;
        m_Rigid_Bodies_Percent.Clear();
    }
    //Get the rigidbodies and mass info from the Ragdoll object
    private bool GetRagdollInfo(GameObject RagdollObject)
    {
        bool isGood = false;

        //Getting all the RB on the object
        m_Rigid_Bodies = m_Ragdoll_Object.GetComponentsInChildren<Rigidbody>();

        if (m_Rigid_Bodies.Length > 0)
        {
            //Adding all the RB masses to m_Total_RB_Mass
            for (int i = 0; i < m_Rigid_Bodies.Length; i++)
            {
                m_Total_RB_Mass += m_Rigid_Bodies[i].mass;
            }
            for (int i = 0; i < m_Rigid_Bodies.Length; i++)
            {
                m_Rigid_Bodies_Percent.Add(m_Rigid_Bodies[i], (m_Rigid_Bodies[i].mass * 100) / m_Total_RB_Mass);
            }
            isGood = true;
        }
        else
            ShowNotification(new GUIContent("No RigidBody in this Object"));

        return isGood;
    }

    //Set new mass on the different RigidBodies, with the good percentage for each
    private bool SetNewMass(float mass)
    {
        bool isGood = false;

        foreach (var item in m_Rigid_Bodies_Percent)
        {
            item.Key.mass = (float)Math.Round(mass * item.Value / 100, 4);
        }

        return isGood;
    }
}