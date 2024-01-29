﻿using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using Object = UnityEngine.Object;

/// <summary>
/// An editor for the <see cref="TabSystem"/>.
/// <br>Allows for dynamic modification &amp; generation on inspector.</br>
/// </summary>
[CustomEditor(typeof(TabSystem)), CanEditMultipleObjects]
internal class TabSystemEditor : Editor
{
    //////////// Object Creation
    [MenuItem("GameObject/UI/Tab System")]
    public static void CreateTabSystem(MenuCommand Command)
    {
        // Create primary gameobject.
        GameObject tabSystem = new GameObject("Tab System");

        // Align stuff
        GameObjectUtility.SetParentAndAlign(tabSystem, (GameObject)Command.context);

        // TabSystem on empty object.
        TabSystem tabSystemScript = tabSystem.AddComponent<TabSystem>();
        // Layout group
        HorizontalLayoutGroup tabSystemLayoutGroup = tabSystem.AddComponent<HorizontalLayoutGroup>();
        tabSystemLayoutGroup.childControlHeight = true;
        tabSystemLayoutGroup.childControlWidth = true;
        tabSystemLayoutGroup.spacing = 10f;
        // Tab Button
        tabSystemScript.CreateTab();

        // Resize stuff accordingly.
        // Width -- Height
        RectTransform tabSystemTransform = tabSystem.GetComponent<RectTransform>();
        tabSystemTransform.sizeDelta = new Vector2(200, 100);

        // Set Unity Stuff
        Undo.RegisterCreatedObjectUndo(tabSystem, string.Format("create {0}", tabSystem.name));
        Selection.activeObject = tabSystem;
    }

    /// <summary>
    /// Record of undo objects to save.
    /// <br>Used for filtering and <see cref="Undo.RecordObjects(Object[], string)"/>.</br>
    /// </summary>
    private readonly List<Object> undoRecord = new List<Object>();

    /// <summary>
    /// Records a generative event for a tab system targets array.
    /// </summary>
    public void UndoRecordGenerativeEvent(Action<TabSystem> generativeEvent, string undoMsg)
    {
        TabSystem[] targets = base.targets.Cast<TabSystem>().ToArray();

        if (undoRecord.Count > 0)
        {
            undoRecord.Clear();
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(undoMsg);
        int undoID = Undo.GetCurrentGroup();

        // to be destroyed / created TabSystem gameobjects
        // Since we are iterating an array of arrays, we don't use the utility method (as this is called one time)
        // Register all buttons into the undo record.
        foreach (TabSystem system in targets)
        {
            foreach (TabButton btn in system.TabButtons)
            {
                if (btn == null)
                {
                    continue;
                }

                undoRecord.Add(btn.gameObject);
            }
        }

        foreach (TabSystem target in targets)
        {
            // Undo.RecordObject does not work
            // Because unity.
            // Undo.RecordObject(target, string.Empty);
            Undo.RegisterCompleteObjectUndo(target, string.Empty);

            if (!PrefabUtility.IsPartOfAnyPrefab(target))
            {
                EditorUtility.SetDirty(target);
            }

            generativeEvent(target);

            if (PrefabUtility.IsPartOfAnyPrefab(target))
            {
                // RegisterCompleteObjectUndo does not immediately add the object into the Undo list
                // So do this to avoid bugs, as this needs to be done after the undo list was updated.

                EditorApplication.delayCall += () =>
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                };
            }
        }

        // Apply serializedObject
        foreach (TabSystem target in targets)
        {
            foreach (TabButton createdUndoRegister in target.TabButtons.Where(tb => !undoRecord.Contains(tb.gameObject)))
            {
                if (createdUndoRegister == null)
                {
                    continue;
                }

                Undo.RegisterCreatedObjectUndo(createdUndoRegister.gameObject, string.Empty);
            }
        }

        Undo.CollapseUndoOperations(undoID);
    }

    public override void OnInspectorGUI()
    {
        // Support multiple object editing
        TabSystem[] targets = base.targets.Cast<TabSystem>().ToArray();
        undoRecord.Clear();

        // PropertyField's (using SerializedObject) are already handled by CanEditMultipleObjects attribute
        // For manual GUI, we need to compensate.
        SerializedObject tabSO = serializedObject;
        bool gEnabled = GUI.enabled;
        bool showMixed = EditorGUI.showMixedValue;
        tabSO.Update();

        // Draw the 'm_Script' field that unity makes (with disabled gui)
        GUI.enabled = false;
        EditorGUILayout.PropertyField(tabSO.FindProperty("m_Script"));
        GUI.enabled = gEnabled;

        // Setup variables
        EditorGUILayout.LabelField("Standard Settings", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal(); // TabButtonAmount
        int tBtnAmountTest = targets[0].TabButtonAmount; // Get a test variable, for showing mixed view.
        EditorGUI.showMixedValue = targets.Any(ts => ts.TabButtonAmount != tBtnAmountTest);

        EditorGUI.BeginChangeCheck();
        bool hasChangedTabButtonAmount = false;
        int TBtnAmount = EditorGUILayout.IntField(nameof(TabSystem.TabButtonAmount), tBtnAmountTest);
        if (GUILayout.Button("+", GUILayout.Width(20f))) { TBtnAmount++; }
        if (GUILayout.Button("-", GUILayout.Width(20f))) { TBtnAmount--; }
        EditorGUI.showMixedValue = showMixed;
        GUILayout.EndHorizontal();
        hasChangedTabButtonAmount = EditorGUI.EndChangeCheck();

        // Show warning if TabButtonAmount is 0 or lower.
        if (targets.Any(tb => tb.TabButtonAmount <= 0))
        {
            string disabledMsg = targets.Length > 1 ? "(Some) TabSystem(s) are disabled. " : "TabSystem is disabled.";
            disabledMsg += "To enable it again set TabButtonAmount to 1 or more.";

            EditorGUILayout.HelpBox(disabledMsg, MessageType.Warning);
        }

        EditorGUI.BeginChangeCheck();
        bool hasChangedCurrentReference = false;
        int tReferenceBtnTest = targets[0].CurrentReferenceTabButton;
        EditorGUI.showMixedValue = targets.Any(ts => ts.TabButtonAmount != tBtnAmountTest);
        int CRefTB = EditorGUILayout.IntField(nameof(TabSystem.CurrentReferenceTabButton), tReferenceBtnTest);
        EditorGUI.showMixedValue = showMixed;
        hasChangedCurrentReference = EditorGUI.EndChangeCheck();

        EditorGUI.BeginChangeCheck();
        bool hasChangedInteractable = false;
        bool tInteractableTest = targets[0].Interactable;
        EditorGUI.showMixedValue = targets.Any(ts => ts.Interactable != tInteractableTest);
        bool TInteractable = EditorGUILayout.Toggle(nameof(TabSystem.Interactable), tInteractableTest);
        EditorGUI.showMixedValue = showMixed;
        hasChangedInteractable = EditorGUI.EndChangeCheck();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fade Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(tabSO.FindProperty(nameof(TabSystem.ButtonFadeType)));
        FadeType tFadeTypeTest = targets[0].ButtonFadeType;
        bool isFadeTypeMixedValue = targets.Any(ts => ts.ButtonFadeType != tFadeTypeTest);

        // Button fade
        // Hide any button fade options if the types are different, otherwise show.
        if (!isFadeTypeMixedValue)
        {
            switch (tFadeTypeTest)
            {
                case FadeType.ColorFade:
                    EditorGUILayout.PropertyField(tabSO.FindProperty(nameof(TabSystem.FadeSpeed)));
                    // Set the default color dynamically on the editor
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(tabSO.FindProperty(nameof(TabSystem.FadeColorTargetDefault)));
                    if (EditorGUI.EndChangeCheck())
                    {
                        // Lazy way of intercepting undo, as property fields don't really like other undos registered after itself
                        foreach (TabSystem target in targets)
                        {
                            Undo.undoRedoPerformed += () =>
                            {
                                target.UpdateButtonAppearances();
                                SceneView.RepaintAll();
                            };

                            target.UpdateButtonAppearances();
                            SceneView.RepaintAll();
                        }
                    }
                    EditorGUILayout.PropertyField(tabSO.FindProperty(nameof(TabSystem.FadeColorTargetHover)));
                    EditorGUILayout.PropertyField(tabSO.FindProperty(nameof(TabSystem.FadeColorTargetClick)));
                    EditorGUILayout.PropertyField(tabSO.FindProperty(nameof(TabSystem.FadeColorTargetDisabled)));
                    EditorGUILayout.PropertyField(tabSO.FindProperty(nameof(TabSystem.FadeSubtractFromCurrentColor)));
                    break;
                case FadeType.SpriteSwap:
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(tabSO.FindProperty(nameof(TabSystem.DefaultSpriteToSwap)));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (TabSystem target in targets)
                        {
                            Undo.undoRedoPerformed += () =>
                            {
                                target.UpdateButtonAppearances();
                                SceneView.RepaintAll();
                            };

                            target.UpdateButtonAppearances();
                            SceneView.RepaintAll();
                        }
                    }
                    EditorGUILayout.PropertyField(tabSO.FindProperty(nameof(TabSystem.HoverSpriteToSwap)));
                    EditorGUILayout.PropertyField(tabSO.FindProperty(nameof(TabSystem.TargetSpriteToSwap)));
                    EditorGUILayout.PropertyField(tabSO.FindProperty(nameof(TabSystem.DisabledSpriteToSwap)));
                    break;
                case FadeType.CustomUnityEvent:
                    EditorGUILayout.PropertyField(tabSO.FindProperty(nameof(TabSystem.ButtonCustomEventOnReset)));
                    EditorGUILayout.PropertyField(tabSO.FindProperty(nameof(TabSystem.ButtonCustomEventOnHover)));
                    EditorGUILayout.PropertyField(tabSO.FindProperty(nameof(TabSystem.ButtonCustomEventOnClick)));
                    break;

                default:
                case FadeType.None:
                    break;
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tab Event", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(tabSO.FindProperty(nameof(TabSystem.OnTabButtonsClicked)));

        if (hasChangedInteractable || hasChangedTabButtonAmount || hasChangedCurrentReference)
        {
            // As an optimization, refrain from executing end change check with arrays as much as possible
            // This will only be possible if we check all tab button amount and interactibility states ofc, which is inconvenient.
            UndoRecordGenerativeEvent((TabSystem target) =>
            {
                if (hasChangedTabButtonAmount && target.TabButtonAmount != TBtnAmount)
                {
                    target.TabButtonAmount = TBtnAmount;
                }
                if (hasChangedInteractable && target.Interactable != TInteractable)
                {
                    target.Interactable = TInteractable;
                    SceneView.RepaintAll(); // Update views instantly
                }

                if (hasChangedCurrentReference)
                {
                    target.CurrentReferenceTabButton = CRefTB;
                }
            }, "change variable on TabSystem");
        }

        tabSO.ApplyModifiedProperties();

        // -- Tab List Actions
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tab List", EditorStyles.boldLabel);

        EditorGUI.indentLevel++; // indentLevel = normal + 1
        GUI.enabled = false;
        // Apparently CanEditMultipleObjects ReorderableList has issues while drawing properties (keeps spamming you should stop calling next)
        // Most likely it uses serializedProperty.arraySize instead of iterating properly so we have to ditch the view if there's more than 2 views
        if (targets.Length <= 1)
        {
            EditorGUILayout.PropertyField(tabSO.FindProperty("tabButtons"));
        }

        GUI.enabled = true;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Tabs"))
        {
            UndoRecordGenerativeEvent((TabSystem target) =>
            {
                target.ClearTabs();
            }, "clear tabs on TabSystem");
        }
        if (GUILayout.Button("Generate Tabs"))
        {
            UndoRecordGenerativeEvent((TabSystem target) =>
            {
                target.GenerateTabs();
            }, "generate tabs on TabSystem");
        }
        if (GUILayout.Button("Reset Tabs"))
        {
            UndoRecordGenerativeEvent((TabSystem target) =>
            {
                target.ResetTabs();
            }, "reset tabs on TabSystem");
        }
        GUILayout.EndHorizontal();

        EditorGUI.indentLevel--; // indentLevel = normal
    }
}
