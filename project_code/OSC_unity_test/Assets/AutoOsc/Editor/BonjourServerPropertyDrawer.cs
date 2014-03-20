//using UnityEditor;
//using UnityEngine;
//
//[CustomPropertyDrawer(typeof(BonjourServer))]
//public class BonjourServerPropertyDrawer : PropertyDrawer {
//
//	public override float GetPropertyHeight (SerializedProperty property, GUIContent label) {
//		return property.isExpanded ? 32f : 16f;
//	}
//
//	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
//
////		position = EditorGUI.IndentedRect(position);
//		position.height = 16;
////		EditorGUI.Foldout(position, property.isExpanded, label, true);
////
//
//		Rect foldoutPosition = position;
//		Rect guiPosition = foldoutPosition;
//		guiPosition.width = 30;
//		foldoutPosition.x -= 14f ;
//		foldoutPosition.x += 44;
//		foldoutPosition.width += 14f;
//		foldoutPosition.width -= 44;
//		label = EditorGUI.BeginProperty(position, label, property);
//		GUI.Button (guiPosition, "add");
//		property.isExpanded = EditorGUI.Foldout(foldoutPosition, property.isExpanded, label, true);
//		EditorGUI.EndProperty();
//		
//		if (!property.isExpanded) {
//			return;
//		}
//		
//		position.y += 16f;
//		SerializedProperty portProp = property.FindPropertyRelative("Port");
//		SerializedProperty addressProp = property.FindPropertyRelative("hostAddress");
//		EditorGUI.LabelField(position, "IP: "+	 addressProp.stringValue + " Port: " + portProp.intValue.ToString());
//
////		EditorGUI.PropertyField(position, property.FindPropertyRelative("Port"));
//	}
//
//}