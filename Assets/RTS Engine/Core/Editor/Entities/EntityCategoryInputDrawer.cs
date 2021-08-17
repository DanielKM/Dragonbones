using RTSEngine.Entities;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RTSEngine.EditorOnly.Entities
{
    [CustomPropertyDrawer(typeof(EntityCategoryInputAttribute))]
    public class EntityCategoryInputDrawer : PropertyDrawer
    {
        private class ViewData
        {
            public int fieldsAmount = 2;
            public bool showEntities = false;
            public int categoryID = 0;
        }

        private Dictionary<string, ViewData> propertyViewData = new Dictionary<string, ViewData>();

        private void Draw (Rect position, SerializedProperty property, GUIContent label, string attributeName, ViewData viewData)
        {
            EntityCategoryInputAttribute customAttribute = attribute as EntityCategoryInputAttribute;

            //to be used for codes and categories
            label = EditorGUI.BeginProperty(position, label, property);

            float height = position.height - EditorGUIUtility.standardVerticalSpacing * viewData.fieldsAmount * 1.5f;

            Rect nextRect = new Rect(position.x, position.y, position.width, height / viewData.fieldsAmount);

            if (property.propertyType != SerializedPropertyType.String)
            {
                viewData.fieldsAmount = 2;
                nextRect.height += height / viewData.fieldsAmount;

                EditorGUI.HelpBox(nextRect, $"Use [{attributeName}] with string fields where you input a category for an entity.", MessageType.Error);
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.PropertyField(nextRect, property, label);

            nextRect.y += nextRect.height + EditorGUIUtility.standardVerticalSpacing;

            if (RTSEditorHelper.GetEntitiesPerCategory() == null)
            {
                viewData.fieldsAmount = 3;
                nextRect.height += height / viewData.fieldsAmount;

                EditorGUI.HelpBox(nextRect, $"Can not fetch the entities placed under the '*/Resources/Prefabs/' path.", MessageType.Error);
                EditorGUI.EndProperty();
                return;
            }

            string[] categories = property.stringValue.Split(',');
            string nextCategory = customAttribute.IsDefiner ? categories[viewData.categoryID] : property.stringValue;

            EditorGUI.indentLevel++;

            if(!RTSEditorHelper.GetEntitiesPerCategory().TryGetValue(nextCategory, out IEnumerable<IEntity> entities))
            {
                if (customAttribute.IsDefiner)
                {
                    RTSEditorHelper.SetEntities();
                    RTSEditorHelper.GetEntitiesPerCategory().TryGetValue(nextCategory, out entities);
                }
                else
                {
                    viewData.fieldsAmount = 3;
                    nextRect.height += height / viewData.fieldsAmount;
                    EditorGUI.HelpBox(
                        new Rect(nextRect.x, nextRect.y + (customAttribute.IsDefiner ? nextRect.height / 2 : 0.0f) + EditorGUIUtility.standardVerticalSpacing, nextRect.width, nextRect.height),
                        $"Entity category: '{nextCategory}' has not been defined for any entity prefab that exists under the '.../Resources/Prefabs' path.", MessageType.Error);
                    EditorGUI.indentLevel--;

                    EditorGUI.EndProperty();
                    return;
                }
            }

            if (entities == null)
                entities = Enumerable.Empty<IEntity>();

            float lastHeight = nextRect.height;
            nextRect.height += height / viewData.fieldsAmount;

            if (customAttribute.IsDefiner)
            {
                viewData.categoryID = EditorGUI.Popup(
                    new Rect(nextRect.x, nextRect.y, nextRect.width, nextRect.height / 2),
                    $"Selected Category:", viewData.categoryID, categories);
            }

            nextRect.y += nextRect.height * (customAttribute.IsDefiner ? 0.5f : 0.0f) + EditorGUIUtility.standardVerticalSpacing;
            nextRect.height = lastHeight;

            viewData.showEntities = EditorGUI.Foldout(nextRect, viewData.showEntities, new GUIContent($"Display category '{nextCategory}' entities (Count: {entities.Count()})"));

            if (viewData.showEntities)
            {
                viewData.fieldsAmount = (customAttribute.IsDefiner ? 3 : 2) + entities.Count();

                EditorGUI.indentLevel++;
                GUI.enabled = false;

                nextRect.y += nextRect.height + EditorGUIUtility.standardVerticalSpacing;

                foreach (IEntity entity in entities)
                {
                    EditorGUI.ObjectField(nextRect, entity.gameObject, typeof(IEntity), false);
                    nextRect.y += nextRect.height + EditorGUIUtility.standardVerticalSpacing;
                }

                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
            else
                viewData.fieldsAmount = (customAttribute.IsDefiner ? 3 : 2);

            EditorGUI.indentLevel--;

            EditorGUI.EndProperty();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!propertyViewData.TryGetValue(property.propertyPath, out ViewData viewData))
            {
                viewData = new ViewData();
                propertyViewData.Add(property.propertyPath, viewData);
            }

            Draw(position, property, label, typeof(EntityCategoryInputAttribute).Name, viewData);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!propertyViewData.TryGetValue(property.propertyPath, out ViewData viewData))
                propertyViewData.Add(property.propertyPath, new ViewData());

            return propertyViewData[property.propertyPath].fieldsAmount * (base.GetPropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing * 1.5f);
        }
    }
}
