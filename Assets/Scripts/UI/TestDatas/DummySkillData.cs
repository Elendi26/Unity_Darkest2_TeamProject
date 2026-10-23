using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DummySkillData
{
    public string SkillName;
    public string SkillExplain;
    public string SkillType;
    public string SkillDetail;
    public Sprite SkillIcon;
    public Sprite SkillTypeIcon;
    public Sprite SkillRangeImage;

    public DummySkillData
        (
            string name,
            string explain,
            string type,
            string detail,
            Sprite icon,
            Sprite typeicon,
            Sprite rangeimage
        )
    {
        SkillName = name;
        SkillExplain = explain;
        SkillType = type;
        SkillDetail = detail;
        SkillIcon = icon;
        SkillTypeIcon = typeicon;
        SkillRangeImage = rangeimage;
    }

}
