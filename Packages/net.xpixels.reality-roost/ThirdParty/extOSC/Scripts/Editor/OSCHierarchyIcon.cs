/* Copyright (c) 2024 dr. ext (Vladimir Sigalkin) */

using UnityEditor;
using UnityEngine;

using extOSC.Core;

namespace extOSC.Editor
{
	[InitializeOnLoad]
	public static class OSCHierarchyIcon
	{
		#region Constructor Methods

		static OSCHierarchyIcon()
		{
#if UNITY_6000_5_OR_NEWER
			EditorApplication.hierarchyWindowItemByEntityIdOnGUI += DrawHierarchyIcon;
#else
			EditorApplication.hierarchyWindowItemOnGUI += DrawHierarchyIcon;
#endif
		}

		#endregion

		#region Private Methods

#if UNITY_6000_5_OR_NEWER
		private static void DrawHierarchyIcon(EntityId entityId, Rect selectionRect)
#else
		private static void DrawHierarchyIcon(int instanceId, Rect selectionRect)
#endif
		{
			if (OSCEditorTextures.IronWall == null) return;

#if UNITY_6000_5_OR_NEWER
			var gameObject = EditorUtility.EntityIdToObject(entityId) as GameObject;
#else
			var gameObject = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
#endif
			if (gameObject == null) return;

			var oscBase = gameObject.GetComponent<OSCBase>();
			if (oscBase == null) return;

			var rect = new Rect(selectionRect.x + selectionRect.width - 18f, selectionRect.y, 16f, 16f);
			GUI.DrawTexture(rect, OSCEditorTextures.IronWallSmall);
		}

		#endregion
	}
}
