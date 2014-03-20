using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(UnityBonjourBrowser))]
public class UnityBonjourBrowserInspector : Editor {
	
	private SerializedObject bonjourBrowser;
	private SerializedProperty availableOscServers;
	private int availableOscServersSize;
	private string availOscServersLabel = "Available Osc servers registered on this network:";
	private string availOscServerString = "availableOscServers";
	void OnEnable () {
		bonjourBrowser = new SerializedObject(target);
		availableOscServers = bonjourBrowser.FindProperty(availOscServerString);
	}

	public override void OnInspectorGUI () {
//		EditorGUIUtility.LookLikeInspector();
		bonjourBrowser.Update();

		DrawIPAddresses();
		bonjourBrowser.ApplyModifiedProperties();
	}

	public void DrawIPAddresses()
	{
		GUILayout.Label(availOscServersLabel);
		EditorGUI.indentLevel++;
		for (int i = 0; i < availableOscServers.arraySize; i++)
		{
			SerializedProperty elementProperty = availableOscServers.GetArrayElementAtIndex(i);
			Rect drawZone = GUILayoutUtility.GetRect(0f, 16f);
			SerializedProperty portProp = elementProperty.FindPropertyRelative("Port");
			SerializedProperty addressProp = elementProperty.FindPropertyRelative("HostAddress");
			EditorGUI.PropertyField(drawZone, elementProperty); 
			drawZone = GUILayoutUtility.GetRect(0f, 16f);
			EditorGUI.LabelField(drawZone, "IP: "+	 addressProp.stringValue + " Port: " + portProp.intValue.ToString());

		}
		EditorGUI.indentLevel--;
	}

}
