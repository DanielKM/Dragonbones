using UnityEngine;
using UnityEditor;

using RTSEngine.EntityComponent;
using RTSEngine.Upgrades;

namespace RTSEngine.EditorOnly.EntityComponent
{
    [CustomPropertyDrawer(typeof(UpgradeTask))]
    public class UpgradeTaskDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Upgrade upgrade = property.FindPropertyRelative("prefabObject").objectReferenceValue.IsValid()
                ? (property.FindPropertyRelative("prefabObject").objectReferenceValue as GameObject).GetComponent<Upgrade>()
                : null;

            string taskTitle = "Upgrade: ";

            if (!upgrade.IsValid())
                taskTitle += "Prefab Unassigned";
            else
            {
                if (upgrade is EntityUpgrade)
                {
                    var entityUpgrade = (upgrade as EntityUpgrade);
                    if (entityUpgrade.SourceEntity.IsValid())
                        taskTitle += $"{entityUpgrade.SourceCode} -> ";
                    else
                        taskTitle += "Source Missing -> ";

                    if (entityUpgrade.UpgradeTarget.IsValid())
                        taskTitle += $"{entityUpgrade.UpgradeTarget.Code}";
                    else
                        taskTitle += "Target Missing";
                }


                else if (upgrade is EntityComponentUpgrade)
                {
                    var entityCompUpgrade = (upgrade as EntityComponentUpgrade);
                    if (entityCompUpgrade.SourceEntity.IsValid() && entityCompUpgrade.SourceComponent.IsValid())
                        taskTitle += $"{entityCompUpgrade.SourceEntity.Code} ({entityCompUpgrade.SourceCode} -> ";
                    else
                        taskTitle += "Source Missing -> ";

                    if (entityCompUpgrade.UpgradeTarget.IsValid())
                        taskTitle += $"{entityCompUpgrade.UpgradeTarget.Code})";
                    else
                        taskTitle += "Target Missing";
                }
            }

            property
                .FindPropertyRelative("taskTitle")
                .stringValue = taskTitle;

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property);
        }
    }
}
