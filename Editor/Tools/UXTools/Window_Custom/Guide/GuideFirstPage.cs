using System.Collections.Generic;
using JetBrains.Annotations;
using ThunderFireUITool;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class GuideFirstPage : VisualElement
{
    public GuideFirstPage(VisualElement parent)
    {
        style.width = 1120;
        style.height = 500;

        var rowContent = UXBuilder.Row(this, new UXBuilderRowStruct()
        {
            align = Align.Center,
            justify = Justify.Center,
            style = new UXStyle() { height = 391 }
        });

        var rowBottom = UXBuilder.Row(this, new UXBuilderRowStruct()
        {
            align = Align.Center,
            justify = Justify.FlexEnd,
            style = new UXStyle() { height = 39 }
        });

        var image = new Image()
        { style = { width = 952, height = 391 } };
        image.image =
            AssetDatabase.LoadAssetAtPath<Texture>(
                "Assets/UXTools/Res/UX-GUI-Editor-Tools/Assets/Editor/Res/Icon/ToolGuide_Chinese.png");
        image.scaleMode = ScaleMode.ScaleToFit;
        rowContent.Add(image);

        UXBuilder.Button(rowBottom, new UXBuilderButtonStruct()
        {
            type = ButtonType.Text,
            text = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_下一步设置功能开关),
            style = new UXStyle() { fontSize = 16, unityFontStyleAndWeight = FontStyle.Italic, marginRight = 17 },
            OnClick = () =>
            {
                GuideWindow.pageNum = 1;
                GuideWindow.GetInstance().DrawPage(parent);
            }
        });
    }

}