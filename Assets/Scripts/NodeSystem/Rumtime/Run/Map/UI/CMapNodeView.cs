using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CMapNodeView : MonoBehaviour
{
    #region 인스펙터
    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;
    [SerializeField] private Image _highlight;          // 현재 위치·선택 가능 표시 테두리
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Color _visitedTint = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color _currentColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color _selectableColor = new Color(1f, 1f, 1f, 0.7f);
    #endregion

    #region 내부 변수
    private CNodeInstance _node;
    private Action<CNodeInstance> _onClicked;
    #endregion

    // 이 뷰가 보여주는 노드
    public CNodeInstance Node => _node;
    // 배치용 RectTransform
    public RectTransform Rect => (RectTransform)transform;

    private void Awake()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }
    }

    private void OnEnable()
    {
        if (_button != null)
        {
            _button.onClick.AddListener(NotifyClicked);
        }
    }

    private void OnDisable()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(NotifyClicked);
        }
    }

    public void Bind(CNodeInstance node, Action<CNodeInstance> onClicked)
    {
        _node = node;
        _onClicked = onClicked;

        if (_label != null)
        {
            _label.text = node != null ? node.Id.ToString() : string.Empty;
        }
    }

    public void Refresh(Sprite icon, bool isVisited, bool isCurrent, bool isSelectable)
    {
        if (_icon != null)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _icon.color = isVisited && !isCurrent ? _visitedTint : Color.white;
        }

        if (_highlight != null)
        {
            _highlight.enabled = isCurrent || isSelectable;
            _highlight.color = isCurrent ? _currentColor : _selectableColor;
        }

        if (_button != null)
        {
            _button.interactable = isSelectable;
        }
    }

    private void NotifyClicked()
    {
        _onClicked?.Invoke(_node);
    }
}
