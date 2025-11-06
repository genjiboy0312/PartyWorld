using UnityEditor;
using UnityEngine;


//-------------------------------------------------------------
    //--APR Player
    //--Editor - Player Binder
    //
    //--Unity Asset Store - Version 1.0
    //
    //--By The Famous Mouse
    //
    //--Twitter @FamousMouse_Dev
    //--Youtube TheFamouseMouse
    //-------------------------------------------------------------


namespace ARP.APR.Scripts.Editor
{
	public class APR_Player_Binder : EditorWindow
	{
    
		//Editor window
		public Texture tex;
		private static APR_Player_Binder _instance;
    
		//Container
		private GameObject refContainer;
		private GameObject Container;
	
		//COMP
		private GameObject refCOMP;
	
		//Root
		private GameObject refRoot;
		private GameObject RootChildren;
		private GameObject Root;
	
		//Body
		private GameObject refBody;
		private GameObject BodyChildren;
		private GameObject Body;
	
		//Head
		private GameObject refHead;
		private GameObject HeadChildren;
		private GameObject Head;
	
		//UpperRightArm
		private GameObject refUpperRightArm;
		private GameObject UpperRightArmChildren;
		private GameObject UpperRightArm;
	
		//LowerRightArm
		private GameObject refLowerRightArm;
		private GameObject LowerRightArmChildren;
		private GameObject LowerRightArm;
    
		//RightHand
		private GameObject refRightHand;
		private GameObject RightHandChildren;
		private GameObject RightHand;
	
		//UpperLeftArm
		private GameObject refUpperLeftArm;
		private GameObject UpperLeftArmChildren;
		private GameObject UpperLeftArm;
	
		//LowerLeftArm
		private GameObject refLowerLeftArm;
		private GameObject LowerLeftArmChildren;
		private GameObject LowerLeftArm;
    
		//RightHand
		private GameObject refLeftHand;
		private GameObject LeftHandChildren;
		private GameObject LeftHand;
	
		//UpperRightLeg
		private GameObject refUpperRightLeg;
		private GameObject UpperRightLegChildren;
		private GameObject UpperRightLeg;
	
		//LowerRightLeg
		private GameObject refLowerRightLeg;
		private GameObject LowerRightLegChildren;
		private GameObject LowerRightLeg;
	
		//RightFoot
		private GameObject refRightFoot;
		private GameObject RightFootChildren;
		private GameObject RightFoot;
	
		//UpperLeftLeg
		private GameObject refUpperLeftLeg;
		private GameObject UpperLeftLegChildren;
		private GameObject UpperLeftLeg;
	
		//LowerLeftLeg
		private GameObject refLowerLeftLeg;
		private GameObject LowerLeftLegChildren;
		private GameObject LowerLeftLeg;
	
		//LeftFoot
		private GameObject refLeftFoot;
		private GameObject LeftFootChildren;
		private GameObject LeftFoot;
	
		[MenuItem("APR Player/APR Player Binder")]
		static void APRPlayerBinderWindow()
		{
			if(_instance == null)
			{
				APR_Player_Binder window = ScriptableObject.CreateInstance(typeof(APR_Player_Binder)) as APR_Player_Binder;
				window.maxSize = new Vector2(350f, 640f);
				window.minSize = window.maxSize;
				window.ShowUtility();
			}
		}
    
		void OnEnable()
		{
			_instance = this;
		}
    
	
		void TutorialLink()
		{
			Help.BrowseURL("https://www.youtube.com/watch?v=FRGplDfQgLE");
		}
	
		void OnGUI()
		{
			GUI.skin.label.wordWrap = true;
			GUILayout.ExpandWidth (false);
		
			EditorGUILayout.Space();
			GUILayout.Label(tex);
		
			EditorGUILayout.Space();
			GUILayout.Label("Import the APR_Player into the scene, align and scale it to fit your model, then link the respective bones of your model below");
        
			EditorGUILayout.Space();
			GUILayout.Label("Note: The APR_Player box model will represent your colliders as well");
		
			EditorGUILayout.Space();
			GUILayout.Label("Here is a tutorial video link of this process:");
			if(GUILayout.Button("Watch Tutorial Video"))
			{
				TutorialLink();
			}
		
		
		
			//New Model fields
			EditorGUILayout.Space();
			EditorGUILayout.Space();
		
			//Container
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Model Container");
			Container = (GameObject)EditorGUILayout.ObjectField(Container, typeof(GameObject), true, GUILayout.Width(180));
			EditorGUILayout.EndHorizontal();
			//Root
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Root Bone");
			Root = (GameObject)EditorGUILayout.ObjectField(Root, typeof(GameObject), true, GUILayout.Width(180));
			EditorGUILayout.EndHorizontal();
			//body
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Body Bone");
			Body = (GameObject)EditorGUILayout.ObjectField(Body, typeof(GameObject), true, GUILayout.Width(180));
			EditorGUILayout.EndHorizontal();
			//Head
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Head Bone");
			Head = (GameObject)EditorGUILayout.ObjectField(Head, typeof(GameObject), true, GUILayout.Width(180));
			EditorGUILayout.EndHorizontal();
			//UpperRightArm
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Upper Right Arm Bone");
			UpperRightArm = (GameObject)EditorGUILayout.ObjectField(UpperRightArm, typeof(GameObject), true, GUILayout.Width(180));
			EditorGUILayout.EndHorizontal();
			//LowerRightArm
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Lower Right Arm Bone");
			LowerRightArm = (GameObject)EditorGUILayout.ObjectField(LowerRightArm, typeof(GameObject), true, GUILayout.Width(180));
			EditorGUILayout.EndHorizontal();
			//RightHand
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Right Hand Bone");
			RightHand = (GameObject)EditorGUILayout.ObjectField(RightHand, typeof(GameObject), true, GUILayout.Width(180));
			EditorGUILayout.EndHorizontal();
			//UpperLeftArm
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Upper Left Arm Bone");
			UpperLeftArm = (GameObject)EditorGUILayout.ObjectField(UpperLeftArm, typeof(GameObject), true, GUILayout.Width(180));
			EditorGUILayout.EndHorizontal();
			//LowerLeftArm
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Lower Left Arm Bone");
			LowerLeftArm = (GameObject)EditorGUILayout.ObjectField(LowerLeftArm, typeof(GameObject), true, GUILayout.Width(180));
			EditorGUILayout.EndHorizontal();
			//LeftHand
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Left Hand Bone");
			LeftHand = (GameObject)EditorGUILayout.ObjectField(LeftHand, typeof(GameObject), true, GUILayout.Width(180));
			EditorGUILayout.EndHorizontal();
			//UpperRightLeg
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Upper Right Leg Bone");
			UpperRightLeg = (GameObject)EditorGUILayout.ObjectField(UpperRightLeg, typeof(GameObject), true, GUILayout.Width(180));
			EditorGUILayout.EndHorizontal();
			//LowerRightLeg
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Lower Right Leg Bone");
			LowerRightLeg = (GameObject)EditorGUILayout.ObjectField(LowerRightLeg, typeof(GameObject), true, GUILayout.Width(180));
			EditorGUILayout.EndHorizontal();
			//RightFoot
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Right Foot Bone");
			RightFoot = (GameObject)EditorGUILayout.ObjectField(RightFoot, typeof(GameObject), true, GUILayout.Width(180));
			EditorGUILayout.EndHorizontal();
			//UpperLeftLeg
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Upper Left Leg Bone");
			UpperLeftLeg = (GameObject)EditorGUILayout.ObjectField(UpperLeftLeg, typeof(GameObject), true, GUILayout.Width(180));
			EditorGUILayout.EndHorizontal();
			//LowerLeftLeg
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Lower Left Leg Bone");
			LowerLeftLeg = (GameObject)EditorGUILayout.ObjectField(LowerLeftLeg, typeof(GameObject), true, GUILayout.Width(180));
			EditorGUILayout.EndHorizontal();
			//LeftFoot
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Left Foot Bone");
			LeftFoot = (GameObject)EditorGUILayout.ObjectField(LeftFoot, typeof(GameObject), true, GUILayout.Width(180));
			EditorGUILayout.EndHorizontal();
		
			//Button
			EditorGUILayout.Space();
			EditorGUILayout.Space();
		
			if(GUILayout.Button("Bind Active Physics Ragdoll Player"))
			{
				BindRagdoll();
			}
		}

        void BindRagdoll()
        {
            refContainer = GameObject.Find("APR_Player");
            refCOMP = GameObject.Find("APR_COMP");
            refRoot = GameObject.Find("APR_Root");
            refBody = GameObject.Find("APR_Body");
            refHead = GameObject.Find("APR_Head");
            refUpperRightArm = GameObject.Find("APR_UpperRightArm");
            refLowerRightArm = GameObject.Find("APR_LowerRightArm");
            refRightHand = GameObject.Find("APR_RightHand");
            refUpperLeftArm = GameObject.Find("APR_UpperLeftArm");
            refLowerLeftArm = GameObject.Find("APR_LowerLeftArm");
            refLeftHand = GameObject.Find("APR_LeftHand");
            refUpperRightLeg = GameObject.Find("APR_UpperRightLeg");
            refLowerRightLeg = GameObject.Find("APR_LowerRightLeg");
            refRightFoot = GameObject.Find("APR_RightFoot");
            refUpperLeftLeg = GameObject.Find("APR_UpperLeftLeg");
            refLowerLeftLeg = GameObject.Find("APR_LowerLeftLeg");
            refLeftFoot = GameObject.Find("APR_LeftFoot");

            // ✅ 필수 오브젝트 확인
            if (refContainer == null || Root == null)
            {
                Debug.LogError("APR_Player 혹은 Root 오브젝트가 비어 있습니다. 필드를 모두 지정했는지 확인하세요.");
                return;
            }

            // ✅ Prefab 해제 시 null 체크 추가
            if (PrefabUtility.IsPartOfPrefabInstance(refContainer))
                PrefabUtility.UnpackPrefabInstance(refContainer, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            if (Container != null && PrefabUtility.IsPartOfPrefabInstance(Container))
                PrefabUtility.UnpackPrefabInstance(Container, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            // ✅ Null 안전 처리 함수로 묶기
            SafeReparent(refRoot, Root);
            SafeReparent(refBody, Body);
            SafeReparent(refHead, Head);
            SafeReparent(refUpperRightArm, UpperRightArm);
            SafeReparent(refLowerRightArm, LowerRightArm);
            SafeReparent(refRightHand, RightHand);
            SafeReparent(refUpperLeftArm, UpperLeftArm);
            SafeReparent(refLowerLeftArm, LowerLeftArm);
            SafeReparent(refLeftHand, LeftHand);
            SafeReparent(refUpperRightLeg, UpperRightLeg);
            SafeReparent(refLowerRightLeg, LowerRightLeg);
            SafeReparent(refRightFoot, RightFoot);
            SafeReparent(refUpperLeftLeg, UpperLeftLeg);
            SafeReparent(refLowerLeftLeg, LowerLeftLeg);
            SafeReparent(refLeftFoot, LeftFoot);

            // ✅ COMP
            if (refCOMP != null && Root != null)
                refCOMP.transform.parent = Root.transform.root;

            if (Container != null && refContainer != null)
                Container.transform.parent = refContainer.transform;

            Debug.Log("✅ APR_Player has been safely binded.");
            this.Close();
        }

        // 🧩 보조 함수 추가
        void SafeReparent(GameObject refPart, GameObject target)
        {
            if (refPart == null || target == null)
            {
                Debug.LogWarning($"⚠️ {refPart?.name ?? "null"} 혹은 {target?.name ?? "null"} 이(가) 비어 있습니다. 이 파트를 건너뜁니다.");
                return;
            }

            var child = target.transform.gameObject;
            refPart.transform.parent = target.transform.parent;
            child.transform.parent = refPart.transform;

            var meshRenderer = refPart.GetComponent<MeshRenderer>();
            var meshFilter = refPart.GetComponent<MeshFilter>();
            if (meshRenderer) DestroyImmediate(meshRenderer);
            if (meshFilter) DestroyImmediate(meshFilter);
        }


        void OnDisable()
		{
			_instance = null;
		}
	}
}
