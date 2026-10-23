using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CMapLinkView : MonoBehaviour
{
    #region 인스펙터
    [SerializeField] private Image _line;
    [SerializeField] private Image _routeIcon;          // 선 가운데 경로 아이콘 (자식, 비워도 됨)
    [SerializeField] private float _thickness = 6f;
    [SerializeField] private float _nodeGap = 45f;      // 양 끝을 노드 반지름만큼 비운다
    [SerializeField, Range(0f, 1f)] private float _dimAlpha = 0.35f;    // 고를 수 없는 선의 투명도
    #endregion

    #region 내부 변수
    private CNodeLink _link;
    #endregion

    // 이 뷰가 보여주는 연결
    public CNodeLink Link => _link;

    public void Bind(CNodeLink link, Vector2 from, Vector2 to)
    {
        _link = link;

        RectTransform rect = (RectTransform)transform;
        Vector2 dir = to - from;
        float length = Mathf.Max(0f, dir.magnitude - _nodeGap * 2f);

        rect.anchoredPosition = (from + to) * 0.5f;
        rect.sizeDelta = new Vector2(length, _thickness);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        // 선과 같이 돌아간 아이콘을 다시 똑바로 세운다
        if (_routeIcon != null)
        {
            _routeIcon.rectTransform.localRotation = Quaternion.Inverse(rect.localRotation);
        }
    }

    public void Refresh(Color color, Sprite icon, bool isSelectable)
    {
        color.a = isSelectable ? 1f : _dimAlpha;

        if (_line != null)
        {
            _line.color = color;
        }

        if (_routeIcon != null)
        {
            _routeIcon.sprite = icon;
            _routeIcon.enabled = icon != null;
            _routeIcon.color = new Color(1f, 1f, 1f, color.a);
        }
    }
}
