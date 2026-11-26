using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatDirection_UI : MonoBehaviour
{
    #region 인스펙터
    [Header("데미지 출력용 텍스트")]
    [SerializeField] private TextMeshProUGUI _damageText;

    [Header("죽음의 일격 출력 이미지")]
    [SerializeField] private Image _deathblowImage;

    [Header("전투 연출용 캔버스그룹")]
    [SerializeField] private CanvasGroup _directionCanvas;

    [Header("떠오르면서 사라지는 시간")]
    [SerializeField] private float _upSpeed = 1f;

    [Header("투명하게 사라지는 시간")]
    [SerializeField] private float _duration = 1f;

    #endregion

    /// <summary>
    /// 대미지 연출용 함수
    /// </summary>
    /// <param name="damage"></param>
    public void DamageDiretion(float damage)
    {
        if(_deathblowImage != null)
        {
            _deathblowImage.gameObject.SetActive(false);
        }

        if(_damageText != null)
        {
            _damageText.gameObject.SetActive(true);

            if (damage <= 0f)
            {
                _damageText.text = "빗나감!";
                _damageText.color = Color.yellow;
            }

            else
            {
                _damageText.text = Mathf.RoundToInt(damage).ToString();
                _damageText.color = Color.red;
            }
        }

        StartCoroutine(FadeUICoroutine());
    }

    /// <summary>
    /// 죽음의 일격만큼의 공격을 받았을 때 죽음의 일격 이미지 출력 및 연출하는 함수
    /// </summary>
    public void OnDeathblow()
    {
        if(_damageText != null)
        {
            _damageText.gameObject.SetActive(false);
        }

        if(_deathblowImage != null)
        {
            _deathblowImage.gameObject.SetActive(true);
        }

        StartCoroutine(FadeUICoroutine());
    }

    /// <summary>
    /// _duration 시간까지 떠오르게 하면서 점점 투명도를 줄여가는 연출
    /// </summary>
    /// <returns></returns>
    private IEnumerator FadeUICoroutine()
    {
        float time = 0f;

        while(time < _duration)
        {
            time += Time.deltaTime;
            transform.position += Vector3.up * (_upSpeed * Time.deltaTime);

            if(_directionCanvas != null)
            {
                _directionCanvas.alpha = Mathf.Lerp(1f, 0f, time / _duration);
            }

            yield return null;
        }

        // 사라지면 파괴
        Destroy(gameObject);
    }

}
