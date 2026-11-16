#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

[CustomPropertyDrawer(typeof(PolymorphicDropdown))]
public class PolymorphicReferenceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Type baseType = GetTargetBaseType();
        if (baseType == null)
        {
            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndProperty();
            return;
        }

        float singleLineHeight = EditorGUIUtility.singleLineHeight;

        // 상단 드롭다운 버튼 그리기
        Rect buttonRect = new Rect(position.x, position.y, position.width, singleLineHeight);
        string currentTypeName = property.managedReferenceValue != null
            ? property.managedReferenceValue.GetType().Name
            : "None (클릭하여 구현체 선택)";

        if (EditorGUI.DropdownButton(buttonRect, new GUIContent(currentTypeName), FocusType.Keyboard))
        {
            ShowTypeMenu(property, baseType);
        }

        // 객체가 할당되어 있다면 그 아래에 내부 변수들 펼쳐서 그리기
        if (property.managedReferenceValue != null)
        {
            Rect contentRect = new Rect(position.x, position.y + singleLineHeight + 2, position.width, position.height - singleLineHeight - 2);
            EditorGUI.PropertyField(contentRect, property, label, true);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float totalHeight = EditorGUIUtility.singleLineHeight;
        if (property.managedReferenceValue != null)
        {
            totalHeight += EditorGUI.GetPropertyHeight(property, label, true) + 4;
        }
        return totalHeight;
    }

    private Type GetTargetBaseType()
    {
        Type fieldType = fieldInfo.FieldType;

        if (fieldType.IsArray)
        {
            return fieldType.GetElementType();
        }
        if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
        {
            return fieldType.GetGenericArguments()[0];
        }
        return fieldType;
    }

    private void ShowTypeMenu(SerializedProperty property, Type baseType)
    {
        GenericMenu menu = new GenericMenu();

        // None 옵션
        menu.AddItem(new GUIContent("None"), property.managedReferenceValue == null, () => {
            property.managedReferenceValue = null;
            property.serializedObject.ApplyModifiedProperties();
        });

        // 해당 인터페이스를 상속받은 모든 구현체 클래스 찾기
        var derivedTypes = TypeCache.GetTypesDerivedFrom(baseType)
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .ToList();

        foreach (var type in derivedTypes)
        {
            menu.AddItem(new GUIContent(type.Name), false, () => {
                var obj = Activator.CreateInstance(type);
                property.managedReferenceValue = obj;
                property.serializedObject.ApplyModifiedProperties();
            });
        }

        menu.ShowAsContext();
    }
}
#endif